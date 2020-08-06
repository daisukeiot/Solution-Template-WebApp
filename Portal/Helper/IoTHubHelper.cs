using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Portal.Helper
{
    public interface IIoTHubHelper
    {
        Task<bool> AddDevice(string deviceId);
        Task<bool> DeleteDevice(string deviceId);
        Task<Device> GetDevice(string deviceId);
        Task<Twin> GetTwin(string deviceId);
        Task<IEnumerable<SelectListItem>> GetDevices();
        Task<Twin> ConnectDevice(string cs);
        string GetIoTHubName(string connectionString);
    }
    public class IoTHubHelper : IIoTHubHelper
    {
        private readonly RegistryManager _registryManager;
        private readonly ILogger<IoTHubHelper> _logger;
        private readonly AppSettings _appSettings;

        public IoTHubHelper(IOptions<AppSettings> config, ILogger<IoTHubHelper> logger)
        {
            _logger = logger;
            _appSettings = config.Value;
            _registryManager = RegistryManager.CreateFromConnectionString(_appSettings.IoTHub.ConnectionString);
        }

        public async Task<Device> GetDevice(string deviceId)
        {
            return await _registryManager.GetDeviceAsync(deviceId);
        }

        public async Task<Twin> GetTwin(string deviceId)
        {
            return await _registryManager.GetTwinAsync(deviceId);
        }

        //public async Task<bool> AddDevice(Guid deviceId, string jsonTags, string jsonDesired)
        public async Task<bool> AddDevice(string deviceId)
        {
            try
            {
                Device device = await _registryManager.GetDeviceAsync(deviceId.ToString());
                if (device == null)
                {
                    _logger.LogDebug($"Create new device '{deviceId}'");
                    device = await _registryManager.AddDeviceAsync(new Device(deviceId.ToString()));
                }
                else
                {
                    _logger.LogDebug($"The device '{deviceId}' already exist.");
                }
            }
            //catch (DeviceAlreadyExistsException)
            //{
            //    device = await _registryManager.GetDeviceAsync(deviceId);
            //}
            catch (Exception e)
            {
                _logger.LogError($"CreateDevice: {e.Message}");
                return false;
            }
            return true;
        }

        public async Task<bool> DeleteDevice(string deviceId)
        {
            try
            {
                await _registryManager.RemoveDeviceAsync(deviceId.ToString());
            }
            catch (Exception e)
            {
                _logger.LogError($"CreateDevice: {e.Message}");
                return false;
            }
            return true;
        }

        public async Task<IEnumerable<SelectListItem>> GetDevices()
        {
            List<SelectListItem> deviceList = new List<SelectListItem>();
            // add empty one
            deviceList.Add(new SelectListItem { Value = "", Text = "" });

            IQuery query = _registryManager.CreateQuery("select * from devices");

            while (query.HasMoreResults)
            {
                var twins = await query.GetNextAsTwinAsync().ConfigureAwait(false);
                foreach (var twin in twins)
                {
                    deviceList.Add(new SelectListItem { Value = twin.DeviceId, Text = twin.DeviceId });
                    Console.WriteLine(twin.DeviceId);
                }
            }
            return deviceList;
        }

        public string GetIoTHubName(string connectionString)
        {
            return connectionString.Split(';')[0].Split('=')[1];
        }

        public async Task<Twin> ConnectDevice(string cs)
        {
            const string modelId = "dtmi:com:example:Thermostat;1";

            var options = new ClientOptions
            {
                ModelId = modelId,
            };

            var deviceClient = DeviceClient.CreateFromConnectionString(cs, Microsoft.Azure.Devices.Client.TransportType.Mqtt, options);

            deviceClient.SetConnectionStatusChangesHandler((status, reason) =>
            {
                _logger.LogDebug($"Connection status change registered - status={status}, reason={reason}.");
            });

            var twin = await deviceClient.GetTwinAsync();

            var message = new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes("{\"Web Client\":\"Connected\"}"));
            message.ContentType = "application/json";
            message.ContentEncoding = "utf-8";
            await deviceClient.SendEventAsync(message);

            await Task.Delay(3000);

            await deviceClient.CloseAsync();

            deviceClient.Dispose();
            return twin;

        }
    }
}
