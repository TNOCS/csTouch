using System.ComponentModel;

namespace csShared.Interfaces
{
  public interface IPlugin : INotifyPropertyChanged
  {
    string Name { get; }     
    AppStateSettings AppState { get; set; }
    bool IsRunning { get; set; }
    string Icon { get; }
    int Priority { get; }
    IPluginScreen Screen { get; set; }
    
    bool HideFromSettings { get; set; }   
    bool CanStop { get; }
    ISettingsScreen Settings { get; set; }

    void Init();
    void Start();
    void Pause();
    void Stop();
  }
}
