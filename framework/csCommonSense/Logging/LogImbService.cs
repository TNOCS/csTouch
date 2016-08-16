
using DataServer;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace csCommon.Logging
{
    internal class LogImbService
    {
        #region Static Fields

        /// <summary>
        ///     The m log.
        /// </summary>
        private static readonly ILog mLog = LogManager.GetLogger("ImbServiceCommunicationLogger");

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Simple logger for CommonSense. For advanced logging use Logger.cs 
        /// </summary>
        static LogImbService()
        {
            // See 
            // <appSettings>
            // <add key = "log4net.Config" value = "log4net.config" />
            // <add key = "log4net.Config.Watch" value = "True" />
            // </appSettings>


        }

        #endregion

        #region Methods

        /// <summary>
        /// The log error.
        /// </summary>
        /// <param name="pMessage">
        /// The p message.
        /// </param>
        internal static void LogError(string pMessage)
        {
            if (mLog.IsErrorEnabled)
            {
                mLog.Error(pMessage ?? "-");
            }
        }

        /// <summary>
        /// The log exception.
        /// </summary>
        /// <param name="pMessage">
        /// The p message.
        /// </param>
        /// <param name="pEx">
        /// The p ex.
        /// </param>
        internal static void LogException(string pMessage, Exception pEx,
            [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = 0)
        {
            if (mLog.IsErrorEnabled)
            {
                mLog.Error(pMessage ?? "-", pEx);
            }

            Debug.Assert(false, (pEx != null) ? pEx.Message : pMessage);
        }

        /// <summary>
        /// The log message.
        /// </summary>
        /// <param name="pMessage">
        /// The p message.
        /// </param>
        internal static void LogMessage(Service pService, string pMessage, [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = 0)
        {
            if (mLog.IsInfoEnabled)
            {
                mLog.Info(String.Format("Service '{0}'({1}); {2}", pService.Name, pService.Id, pMessage ?? "-"));
            }
        }

        /// <summary>
        /// The log message.
        /// </summary>
        /// <param name="pMessage">
        /// The p message.
        /// </param>
        internal static void LogWarning(string pMessage, [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = 0)
        {
            if (mLog.IsWarnEnabled)
            {
                mLog.Warn(pMessage ?? "-");
            }
        }

        internal static void ToFile(string pLocation, string pContent)
        {
            if (mLog.IsInfoEnabled)
            {
                mLog.Info(pLocation + System.Environment.NewLine + ": " + pContent);
            }
        }


        #endregion
    }
}
