using Caliburn.Micro;
using csShared;
using csShared.Interfaces;
using System.ComponentModel.Composition;

namespace csCommon.MapPlugins.StartTabPanel
{
    [Export(typeof(IPlugin))]
    public class StartTabPanelPlugin : PropertyChangedBase, IPlugin
    {
        public bool CanStop { get { return false; } }

        private ISettingsScreen _settings;

        public ISettingsScreen Settings
        {
            get { return _settings; }
            set { _settings = value; NotifyOfPropertyChange(() => Settings); }
        }

        private IPluginScreen _screen = new StartTabPanelViewModel(AppStateSettings.Instance.Container);

        public IPluginScreen Screen
        {
            get { return _screen; }
            set { _screen = value; NotifyOfPropertyChange(() => Screen); }
        }

        private bool _hideFromSettings = true;

        public bool HideFromSettings
        {
            get { return _hideFromSettings; }
            set { _hideFromSettings = value; NotifyOfPropertyChange(() => HideFromSettings); }
        }

        public int Priority
        {
            get { return 1; }
        }

        public string Icon
        {
            get { return @"icons\compass.png"; }
        }

        private bool _isRunning;

        public bool IsRunning
        {
            get { return _isRunning; }
            set { _isRunning = value; NotifyOfPropertyChange(() => IsRunning); }
        }

        public AppStateSettings AppState { get; set; }

        public FloatingElement Element { get; set; }

       
        public string Name
        {
            get { return "StartTabPanel"; }
        }

        public void Init()
        {
            //((StartTabPanelViewModel) Screen).UpdateLayout();
            //((StartTabPanelViewModel) Screen).Init();
        }



        public void Start()
        {
            IsRunning = true;
        }

        public void Pause()
        {
            IsRunning = false;
            
        }

        public void Stop()
        {
            IsRunning = false;            
        }
    }
}
