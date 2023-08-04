using log4net;
using log4net.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Reflection;
using WorkerService.Model;
using System.Configuration.Install;
using System;

namespace WorkerService
{
    public class Program
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static ServiceInfo serviceInfo;
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            ConfigureLogging();
            var builder = CreateHostBuilder(args);
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
            serviceInfo = configuration.GetSection("ServiceInfo").Get<ServiceInfo>();
            if (args.Length > 0 && args[0].ToLower() == "-i")
                Install();
            else if (args.Length > 0 && args[0].ToLower() == "-u")
                Uninstall();
            else
                builder.Build().Run();
        }
        private static void ConfigureLogging()
        {
            var dirname = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            XmlConfigurator.Configure(new FileInfo(string.Format("{0}{1}", dirname, @"\Config\log4Net.config")));
        }
        private static void Install()
        {
            ManagedInstallerClass.InstallHelper(new[] { Assembly.GetExecutingAssembly().Location });
            _logger.InfoFormat("Install successful");
        }
        private static void Uninstall()
        {
            ManagedInstallerClass.InstallHelper(new[] { "/u", Assembly.GetExecutingAssembly().Location });
            _logger.InfoFormat("Uninstall successful");
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
                           services.AddHostedService<TimerWorkerService>();
                       });
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _logger.Error(e.ExceptionObject.ToString());
        }
    }
}
