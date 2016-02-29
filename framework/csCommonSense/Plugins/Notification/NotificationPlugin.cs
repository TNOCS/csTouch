using Caliburn.Micro;
using csShared;
using csShared.Interfaces;

namespace csCommon
{
    using System.ComponentModel.Composition;

    [Export(typeof(IPlugin))]
    public class NotificationPlugin : PropertyChangedBase, IPlugin
    {
        public bool CanStop { get { return true; } }

        private ISettingsScreen _settings;

        public ISettingsScreen Settings
        {
            get { return _settings; }
            set { _settings = value; NotifyOfPropertyChange(() => Settings); }
        }
        private IPluginScreen _screen = new NotificationViewModel();

        public IPluginScreen Screen
        {
            get { return _screen; }
            set { _screen = value; NotifyOfPropertyChange(() => Screen); }
        }

        private bool _hideFromSettings;

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
            get { return @"/csCommon;component/Resources/Icons/notification.png"; }
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
            get { return "Notification"; }
        }

        public void Init()
        {       
            
        }

      
        public void Clear()
        {
            ((NotificationViewModel) _screen).EndNotifications();
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
