using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

namespace IoTHubTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            RegistryManager registryManager = RegistryManager.CreateFromConnectionString("HostName=Hub-7kgvf.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=zGyXv4g76zsuHukrxCj4eH1RkGhLopnwOSvTiNM2pt8=");
            IQuery query = registryManager.CreateQuery("select * from devices");

            while(query.HasMoreResults)
            {
                var devices = await query.GetNextAsTwinAsync().ConfigureAwait(false);
                foreach (var device in devices)
                {
                    Console.WriteLine(device.DeviceId);
                }
            }
        }
    }
}
