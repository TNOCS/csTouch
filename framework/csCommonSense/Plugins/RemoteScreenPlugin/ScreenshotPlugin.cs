using System.Windows;
using Caliburn.Micro;
using csImb;
using csShared;
using csShared.Interfaces;

namespace csCommon
{
    using System.ComponentModel.Composition;

    [Export(typeof(IPlugin))]
    public class ScreenshotPlugin : PropertyChangedBase, IPlugin
    {

        public bool CanStop { get { return true; } }

        private ISettingsScreen settings;

        public ISettingsScreen Settings
        {
            get { return settings; }
            set { settings = value; NotifyOfPropertyChange(() => Settings); }
        }
        private bool hideFromSettings;

        public bool HideFromSettings
        {
            get { return hideFromSettings; }
            set { hideFromSettings = value; NotifyOfPropertyChange(() => HideFromSettings); }
        }

        private IPluginScreen screen;

        public IPluginScreen Screen
        {
            get { return screen; }
            set { screen = value; NotifyOfPropertyChange(() => Screen); }
        }

        public int Priority
        {
            get { return 1000; }
        }
        Screenshots _s;

        public string Icon
        {
            get { return "/csCommon;component/Resources/Icons/Camerawhite.png"; }
        }

        public AppStateSettings AppState { get; set; }

       
        private bool isRunning;

        public bool IsRunning
        {
            get { return isRunning; }
            set { isRunning = value; NotifyOfPropertyChange(() => IsRunning); }
        }


        public string Name
        {
            get { return "Follow Screen"; }
        }

        public void Init()
        {
            AppState.Imb.Status.AddCapability("Screenshots");
            AppState.Imb.Status.AddCapability("receivescreenshot");            
            //AppState.Imb.CommandReceived += ImbCommandReceived;
        }

        void ImbCommandReceived(object sender, csImb.Command c)
        {
            //switch (c.CommandName)
            //{
            //    case "ScreenOn":
            //        if (!IsRunning)
            //        {
            //            this.ScreenOn();
            //            IsRunning = true;
            //        }
            //        break;
            //    case "ScreenOff":
            //        if (IsRunning)
            //        {
            //            this.ScreenOff();
            //            IsRunning = false;
            //        }
            //        break;
            //}
        }

        public void ScreenOn()
        {
            Execute.OnUIThread(() =>
            {
                _s = new Screenshots { Target = Application.Current.MainWindow, Imb = AppState.Imb };
                _s.Start(AppState.Config.GetInt("Screenshot.Interval",2000));
            });
        }

        public void ScreenOff()
        {
            Execute.OnUIThread(() =>
            {
                if (_s != null) _s.Stop();
            });
        }

        public void Start()
        {
            IsRunning = true;
            ScreenOn();
        }

        public void Pause()
        {
            if (_s!=null) _s.Stop();
        }

        public void Stop()
        {
            IsRunning = false;
            ScreenOff();
        }
    }
}
