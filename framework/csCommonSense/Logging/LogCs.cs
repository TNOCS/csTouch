using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csCommon.Logging
{
    using System.Diagnostics;

    using log4net;
    using log4net.Appender;
    using log4net.Config;
    using log4net.Layout;
    using System.Runtime.CompilerServices;
    /// <summary>
    ///  
    /// </summary>
    internal class LogCs
    {
        #region Static Fields

        /// <summary>
        ///     The m log.
        /// </summary>
        private static readonly ILog mLog = LogManager.GetLogger("CommonSense");

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Simple logger for CommonSense. For advanced logging use Logger.cs 
        /// </summary>
        static LogCs()
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
        internal static void LogMessage(string pMessage, [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = 0)
        {
            if (mLog.IsInfoEnabled)
            {
                mLog.Info(pMessage ?? "-");
            }
        }

        #endregion
    }
}
