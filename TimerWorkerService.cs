using log4net;
using log4net.Config;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using WorkerService.Controller;
using WorkerService.Model;

namespace WorkerService
{
    internal class TimerWorkerService : BackgroundService
    {
        private Log log = new Logger();
        private readonly ILogger<TimerWorkerService> _logger;
        private readonly SqlConnectionFactory _provider;
        private readonly AwsS3Info _awsS3;
        AwsS3Controller awsS3 = new AwsS3Controller();
        public TimerWorkerService(ILogger<TimerWorkerService> logger, SqlConnectionFactory provider, AwsS3Info awsS3Info)
        {
            _logger = logger;
            _awsS3 = awsS3Info;
            _provider = provider;
        }
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We catch anything and alert instead of rethrowing")]
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        _logger.LogInformation("Scan file");
                        log.Info("Scan file");
                        awsS3.UploadFile(_provider.connectionString, _awsS3).Wait();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unhandled exception occurred in the worker. Sending an alert. Worker will retry after the normal interveral.");
                        log.Info(ex.Message);
                    }

                    await Task.Delay(10 * 1000, stoppingToken).ConfigureAwait(false);
                }

                _logger.LogInformation("Execution ended. Cancelation token cancelled = {IsCancellationRequested}",
                    stoppingToken.IsCancellationRequested);
            }
            catch (Exception ex) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Execution Cancelled");
                log.Info(ex.Message);
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandeled exception. Execution Stopping");
                log.Info(ex.Message);
                Environment.Exit(1);
            }
        }
    }
}
