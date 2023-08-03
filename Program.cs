using log4net;
using log4net.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.ServiceProcess;
using System.IO;
using System.Reflection;
using WorkerService.Model;
using System.Collections;

namespace WorkerService
{
    public class Program
    {
        private static ServiceInfo serviceInfo;
        public static void Main(string[] args)
        {
            ConfigureLogging();
            var builder = CreateHostBuilder(args);
            if (args.Length > 0 && args[0].ToLower() == "-i")
                InstallService();
            else if (args.Length > 0 && args[0].ToLower() == "-u")
                UninstallService();
            else
                builder.Build().Run();
        }
        private static void ConfigureLogging()
        {
            var dirname = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            XmlConfigurator.Configure(new FileInfo(string.Format("{0}{1}", dirname, @"\Config\log4net.config")));
        }
        private static void InstallService()
        {
            using (var serviceProcessInstaller = new ServiceProcessInstaller())
            {
                serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
                using (var serviceInstaller = new ServiceInstaller())
                {
                    serviceInstaller.DisplayName = serviceInfo.DisplayName;
                    serviceInstaller.ServiceName = serviceInfo.ServiceName;
                    serviceInstaller.StartType = ServiceStartMode.Automatic;

                    var serviceContext = new System.Configuration.Install.InstallContext();
                    serviceInstaller.Context = serviceContext;

                    var assemblyPath = Assembly.GetExecutingAssembly().Location;
                    serviceInstaller.Parent = serviceProcessInstaller;
                    var state = new Hashtable();
                    serviceInstaller.Install(state);
                }
            }
        }

        private static void UninstallService()
        {
            using (var serviceProcessInstaller = new ServiceProcessInstaller())
            {
                serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
                using (var serviceInstaller = new ServiceInstaller())
                {
                    serviceInstaller.ServiceName = serviceInfo.ServiceName;

                    var serviceContext = new System.Configuration.Install.InstallContext();
                    serviceInstaller.Context = serviceContext;

                    serviceInstaller.Parent = serviceProcessInstaller;
                    serviceInstaller.Uninstall(null);
                }
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
                    Host.CreateDefaultBuilder(args)
                       .ConfigureAppConfiguration(configurationBuilder =>
                        {
                            var dirname = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                            var allConfig = Directory.GetFiles(string.Format("{0}{1}", dirname, @"\Config"), "*.json", SearchOption.AllDirectories);
                            foreach (var jsonFilename in allConfig)
                                configurationBuilder.AddJsonFile(jsonFilename);
                        }).ConfigureServices((hostContext, services) =>
                        {
                            IConfiguration configuration = hostContext.Configuration;
                            SqlConnectionFactory dbInfo = configuration.GetSection("Database").Get<SqlConnectionFactory>();
                            services.AddSingleton(dbInfo);
                            AwsS3Info awsS3Info = configuration.GetSection("AWSS3Info").Get<AwsS3Info>();
                            services.AddSingleton(awsS3Info);
                            serviceInfo = configuration.GetSection("ServiceInfo").Get<ServiceInfo>();
                            services.AddHostedService<TimerWorkerService>();
                        });
    }
}
