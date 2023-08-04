using System;
using System.Collections.Generic;
using System.Text;

namespace WorkerService.Model
{
    public class ServiceInstallConfig : BaseLocalMachineConfig<ServiceInstallConfig>
    {
        private const string KeyDescription = "Description";
        private const string KeyDisplayName = "DisplayName";
        private const string KeyServiceName = "ServiceName";

        public ServiceInstallConfig()
        {
            this.LocalMachineConfigPath = @"Config\ServiceInstallConfig.xml";
        }

        public string Description
        {
            get
            {
                string result = GetConfigValue(KeyDescription, "WorkerService WinService");
                return result;
            }
            set
            {
                SaveConfigValue(KeyDescription, value);
            }
        }

        public string DisplayName
        {
            get
            {
                string result = GetConfigValue(KeyDisplayName, "Chương trình tự động");
                return result;
            }
            set
            {
                SaveConfigValue(KeyDisplayName, value);
            }
        }

        public string ServiceName
        {
            get
            {
                string result = GetConfigValue(KeyServiceName, "WorkerService");
                return result;
            }
            set
            {
                SaveConfigValue(KeyServiceName, value);
            }
        }
    }
}
