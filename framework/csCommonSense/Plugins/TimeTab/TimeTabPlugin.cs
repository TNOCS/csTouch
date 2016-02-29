using System;
using System.ComponentModel.Composition;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using csShared;
using csShared.Controls.SlideTab;
using csShared.Interfaces;
using csShared.TabItems;


namespace csTimeTabPlugin
{
    [Export(typeof(IPlugin))]
    public class TimeTabPlugin : PropertyChangedBase, IPlugin {
        public TimeTabPlugin()
        {
            AppStateSettings.Instance.EventAggregator.Subscribe(this);
        }

        public bool CanStop { get { return true; } }

        private ISettingsScreen settings;

        public ISettingsScreen Settings
        {
            get { return settings; }
            set { settings = value; NotifyOfPropertyChange(() => Settings); }
        }

        private IPluginScreen screen;

        public IPluginScreen Screen
        {
            get { return screen; }
            set { screen = value; NotifyOfPropertyChange(() => Screen); }
        }

        private bool hideFromSettings;

        public bool HideFromSettings
        {
            get { return hideFromSettings; }
            set { hideFromSettings = value; NotifyOfPropertyChange(() => HideFromSettings); }
        }

        public int Priority
        {
            get { return 6; }
        }

        public string Icon
        {
            get { return @"/csCommon;component/Resources/Icons/timetab.png"; }
        }

        #region IPlugin Members

        public string Name
        {
            get { return "TimeTabPlugin"; }
        }

        private bool isRunning;

        public bool IsRunning
        {
            get { return isRunning; }
            set { isRunning = value; NotifyOfPropertyChange(() => IsRunning); }
        }

        public AppStateSettings AppState { get; set; }

        public StartPanelTabItem st { get; set; }

        public void Init()
        {
           
        }

        public void Start()
        {
            var timeTabViewModel = new TimeTabViewModel() { Plugin = this };

            st = new StartPanelTabItem {
                Name = "Background",
                HeaderStyle = TabHeaderStyle.Image,
                Image = new BitmapImage(new Uri("pack://application:,,,/csCommon;component/Resources/Icons/timetab.png")),
                ModelInstance = timeTabViewModel
            };
            AppState.AddStartPanelTabItem(st);
            IsRunning = true;
        }

        public void Pause()
        {
            
        }

        public void Stop()
        {
            IsRunning = false;
            AppState.RemoveStartPanelTabItem(st);
        }
        
        #endregion
    }
}
