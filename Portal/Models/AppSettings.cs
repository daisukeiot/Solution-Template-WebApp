using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Models
{

    public class IotHubSetting
    {
        public string ConnectionString { get; set; }
    }
    public class SignalrSetting
    {
        public string ConnectionString { get; set; }
    }

    public class AppSettings
    {
        public SignalrSetting SignalR { get; set; }
        public IotHubSetting IoTHub { get; set; }
    }
    //public class AzureSetting
    //{
    //    public SignalrSetting SignalR { get; set; }
    //    public IotHubSetting IoTHub { get; set; }
    //}

    //public class AppSettings
    //{
    //    public AzureSetting Azure { get; set;}
    //}
}

