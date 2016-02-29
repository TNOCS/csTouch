using csShared;
using csShared.Interfaces;

namespace csCommon
{
  public interface IFloating
  {
    AppStateSettings AppStateSettings { get; }    
  }

  public interface IPluginView 
  {
    IPlugin Plugin { get; set; }

  }
}
