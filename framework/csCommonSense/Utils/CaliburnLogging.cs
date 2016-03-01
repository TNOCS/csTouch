// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CaliburnLogging.cs" company="">
//   
// </copyright>
// <summary>
//   The caliburn logging.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace csCommon.Utils
{
    #region

    using System;
    using System.Diagnostics;

    using Caliburn.Micro;

    #endregion

    /// <summary>
    /// The caliburn logging.
    /// </summary>
    public class CaliburnLogging : ILog
    {
        #region Fields

        /// <summary>
        /// The _type.
        /// </summary>
        private readonly Type _type;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CaliburnLogging"/> class.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        public CaliburnLogging(Type type)
        {
            this._type = type;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The error.
        /// </summary>
        /// <param name="exception">
        /// The exception.
        /// </param>
        public void Error(Exception exception)
        {
            Debug.WriteLine(this.CreateLogMessage(exception.ToString()), "ERROR");
        }

        /// <summary>
        /// The info.
        /// </summary>
        /// <param name="format">
        /// The format.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        public void Info(string format, params object[] args)
        {
            Debug.WriteLine(this.CreateLogMessage(format, args), "INFO");
        }

        /// <summary>
        /// The warn.
        /// </summary>
        /// <param name="format">
        /// The format.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        public void Warn(string format, params object[] args)
        {
            Debug.WriteLine(this.CreateLogMessage(format, args), "WARN");
        }

        #endregion

        #region Methods

        /// <summary>
        /// The create log message.
        /// </summary>
        /// <param name="format">
        /// The format.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private string CreateLogMessage(string format, params object[] args)
        {
            return string.Format("[{0}] {1}", DateTime.Now.ToString("o"), string.Format(format, args));
        }

        #endregion
    }
}