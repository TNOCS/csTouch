using System;
using System.ComponentModel.Composition;
using System.Windows;
using Caliburn.Micro;
using csCommon.Plugins.CircularMenu;
using csShared;
using csShared.FloatingElements;
using csShared.Interfaces;

namespace csCommon.Plugins.CompassPlugin
{
    [Export(typeof(IPlugin))]
    public class CompassPluginPlugin : PropertyChangedBase, IPlugin
    {
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
            get { return 1; }
        }

        public string Icon
        {
            get { return @"/csCommon;component/Resources/Icons/Compassblack.png"; }
        }

        private bool isRunning;

        public bool IsRunning
        {
            get { return isRunning; }
            set { isRunning = value; NotifyOfPropertyChange(() => IsRunning); }
        }

        public AppStateSettings AppState { get; set; }

        public FloatingElement Element { get; set; }

       

        public string Name
        {
            get { return "CompassPlugin"; }
        }

        public void Init()
        {       
            
        }

      
      

        public void Start()
        {
            IsRunning = true;

            var viewModel = new CompassViewModel() { };
            var start = new Point(300, 300);

            // TODO EV Check, used to be var Element
            Element = FloatingHelpers.CreateFloatingElement("Compass", start, new Size(400, 400), viewModel);
            var res = Application.Current.FindResource("SimpleContainer");
            Element.Style = res as Style;

           
            viewModel.Element = Element;
            Element.CanRotate = false;
            Element.CanScale = false;
            Element.RemoveOnEdge = false;
            Element.StartPosition = start;
            Element.ResetOnEdge = true;
            AppState.FloatingItems.AddFloatingElement(Element);
           



        }

        public void Pause()
        {
            IsRunning = false;
            
        }

        public void Stop()
        {
            IsRunning = false;
            AppState.FloatingItems.RemoveFloatingElement(Element);
        }
    }
}
