using Caliburn.Micro;
using csCommon.Plugins.Timeline;
using csShared;
using csShared.Interfaces;

namespace csCommon
{
    using System.ComponentModel.Composition;

    [Export(typeof(IPlugin))]
    public class TimelinePlugin : PropertyChangedBase, IPlugin
    {
        public bool CanStop { get { return true; } }

        private ISettingsScreen settings;

        public ISettingsScreen Settings
        {
            get { return settings; }
            set { settings = value; NotifyOfPropertyChange(() => Settings); }
        }

        private IPluginScreen screen = new TimelineViewModel();

        public IPluginScreen Screen
        {
            get { return screen; }
            set { screen = value; NotifyOfPropertyChange(() => Screen); }
        }

        private bool hideFromSettings = true;

        public bool HideFromSettings
        {
            get { return hideFromSettings; }
            set { hideFromSettings = value; NotifyOfPropertyChange(() => HideFromSettings); }
        }

        public int Priority
        {
            get { return 1; }
        }

        public string Icon
        {
            get { return @"icons\globe.png"; }
        }

        private bool isRunning;

        public bool IsRunning
        {
            get { return isRunning; }
            set { isRunning = value; NotifyOfPropertyChange(() => IsRunning); }
        }

        public AppStateSettings AppState { get; set; }


        public string Name
        {
            get { return "Timeline"; }
        }

        public void Init()
        {

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
