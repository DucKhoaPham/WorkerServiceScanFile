using Microsoft.Extensions.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using WorkerService.Model;

namespace WorkerService
{
    [RunInstaller(true)]
    public partial class VersionUpdateWinSeviceInstaller : System.Configuration.Install.Installer
    {
        private ServiceProcessInstaller samplingServiceProcessInstaller;

        private ServiceInstaller samplingServiceInstaller;

        public VersionUpdateWinSeviceInstaller()
        {
            InitializeComponent();

            InstallSetup();
        }

        public void InstallSetup()
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            var dirname = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var allConfig = Directory.GetFiles(string.Format("{0}{1}", dirname, @"\Config"), "*.json", SearchOption.AllDirectories);
            foreach (var jsonFilename in allConfig)
                if (Path.GetFileName(jsonFilename) == "appsettings.json")
                {
                    configurationBuilder.AddJsonFile(jsonFilename);
                    break;
                }
            IConfiguration configuration = configurationBuilder.Build();
            var serviceInfo = configuration.GetSection("ServiceInfo").Get<ServiceInfo>();
            this.samplingServiceProcessInstaller = new ServiceProcessInstaller();
            this.samplingServiceInstaller = new ServiceInstaller();
            // 
            // orderEntryServiceProcessInstaller
            // 
            this.samplingServiceProcessInstaller.Account = ServiceAccount.LocalSystem;
            this.samplingServiceProcessInstaller.Password = null;
            this.samplingServiceProcessInstaller.Username = null;
            // 
            // orderEntryServiceInstaller
            // 
            this.samplingServiceInstaller.ServiceName = serviceInfo.ServiceName;
            this.samplingServiceInstaller.DisplayName = serviceInfo.DisplayName;
            this.samplingServiceInstaller.Description = "Chương trình tự động upload file attachment thư viện";
            this.samplingServiceInstaller.StartType = ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            Installers.AddRange(new Installer[] 
            {
				this.samplingServiceProcessInstaller,
				this.samplingServiceInstaller
            });
        }
    }
}
