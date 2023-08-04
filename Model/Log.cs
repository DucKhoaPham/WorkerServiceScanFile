using log4net;
using log4net.Config;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace WorkerService
{
    public interface Log
    {
        void Debug(string msg);
        void Info(string msg);
        void Error(string msg, Exception? ex = null);
    }
    public class Logger : Log
    {

        private readonly ILog _logger;
        public Logger()
        {
            this._logger = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
        }
        public void Debug(string msg)
        {
            this._logger?.Debug(msg);
        }
        public void Info(string msg)
        {
            this._logger?.Info(msg);
        }
        public void Error(string msg, Exception? ex = null)
        {
            this._logger?.Error(msg, ex?.InnerException);
        }
    }
}
