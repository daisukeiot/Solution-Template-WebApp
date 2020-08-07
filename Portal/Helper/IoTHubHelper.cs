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
        Task<Twin> SendTelemetry(string cs);
        string GetIoTHubName(string connectionString);
    }
    public class IoTHubHelper : IIoTHubHelper
    {
        private readonly RegistryManager _registryManager;
        private readonly ILogger<IoTHubHelper> _logger;
        private readonly AppSettings _appSettings;
        private DeviceClient _deviceClient;
        private bool _isConnected;

        public IoTHubHelper(IOptions<AppSettings> config, ILogger<IoTHubHelper> logger)
        {
            _logger = logger;
            _appSettings = config.Value;
            _registryManager = RegistryManager.CreateFromConnectionString(_appSettings.IoTHub.ConnectionString);
            _deviceClient = null;
            _isConnected = false;
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
                    _logger.LogInformation(twin.DeviceId);
                }
            }
            return deviceList;
        }

        public string GetIoTHubName(string connectionString)
        {
            return connectionString.Split(';')[0].Split('=')[1];
        }

        private async void ConnectionStatusChangedHandler(
                    ConnectionStatus status,
                    ConnectionStatusChangeReason reason)
        {
            _logger.LogInformation(
                "Client connection state changed. Status: {status}, Reason: {reason}",
                status,
                reason);

            if (status == ConnectionStatus.Connected)
            {
                _isConnected = true;
            } else
            {
                _isConnected = false;
            }

            //if (status == ConnectionStatus.Disabled && !IsConnInProgress)
            //{
            //    await ConnectToHubAsync(_cancellationToken);
            //}
        }

        public async Task<Twin> ConnectDevice(string cs)
        {
            Twin twin = null;
            const string modelId = "dtmi:com:example:Thermostat;1";

            _logger.LogInformation($"{_isConnected}");

            var options = new ClientOptions
            {
                ModelId = modelId,
            };

            if (_deviceClient == null)
            {
                _deviceClient = DeviceClient.CreateFromConnectionString(cs, Microsoft.Azure.Devices.Client.TransportType.Mqtt, options);

                _deviceClient.SetConnectionStatusChangesHandler(ConnectionStatusChangedHandler);

                await _deviceClient.OpenAsync();

                while (_isConnected == false)
                {
                    await Task.Delay(10000);
                }

                 twin = await _deviceClient.GetTwinAsync();

            } 
            else
            {
                twin = await _deviceClient.GetTwinAsync();

                await _deviceClient.CloseAsync();

                _deviceClient.Dispose();

                _deviceClient = null;
            }

            return twin;
        }

        public async Task<Twin> SendTelemetry(string cs)
        {
            Twin twin;

            if (_deviceClient == null)
            {
                twin = await ConnectDevice(cs);
            } else
            {
                twin = await _deviceClient.GetTwinAsync();
            }

            var message = new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes("{\"Web Client\":\"Connected\"}"));
            message.ContentType = "application/json";
            message.ContentEncoding = "utf-8";
            await _deviceClient.SendEventAsync(message).ConfigureAwait(false);

            return twin;
        }
    }
}
