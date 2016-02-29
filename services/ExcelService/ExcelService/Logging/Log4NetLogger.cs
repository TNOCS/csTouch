using System;

namespace ExcelService.Logging
{
    public class Log4NetLogger : ILog
    {
        private readonly log4net.ILog log;

        public Log4NetLogger(Type loggerType)
        {
            log = log4net.LogManager.GetLogger(loggerType);
        }

        public void Debug(string message, params object[] args)
        {
            log.DebugFormat(message, args);
        }

        public void Info(string message, params object[] args)
        {
            log.InfoFormat(message, args);
        }

        public void Warn(string message, params object[] args)
        {
            log.WarnFormat(message, args);
        }

        public void Error(string message, params object[] args)
        {
            log.ErrorFormat(message, args);
        }

        public void Fatal(string message, params object[] args)
        {
            log.FatalFormat(message, args);
        }

        public bool IsDebugEnabled
        {
            get { return log.IsDebugEnabled; }
        }
    }
}
