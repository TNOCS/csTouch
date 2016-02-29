using System;
using System.Diagnostics;
using Caliburn.Micro;

namespace csShared.Utils
{
  public class DebugLogger : ILog
  {
    #region Fields
    private readonly Type type;
    #endregion

    #region Constructors
    public DebugLogger(Type type)
    {
      this.type = type;
    }
    #endregion

    #region Helper Methods
    private string CreateLogMessage(string format, params object[] args) {
      return string.Format("[{0}-{1}] {2}", type.FullName, DateTime.Now.ToString("o"), args.Length == 0 ? format : string.Format(format, args));
    }
    #endregion

    #region ILog Members
    public void Error(Exception exception)
    {
      Debug.WriteLine(CreateLogMessage(exception.ToString()), "ERROR");
    }
    public void Info(string format, params object[] args)
    {
      Debug.WriteLine(CreateLogMessage(format, args), "INFO");
    }
    public void Warn(string format, params object[] args)
    {
      Debug.WriteLine(CreateLogMessage(format, args), "WARN");
    }
    #endregion
  }
}
