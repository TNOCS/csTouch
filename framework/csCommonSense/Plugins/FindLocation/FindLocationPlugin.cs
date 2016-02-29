using System.Windows.Input;
using Caliburn.Micro;
using csShared;
using System.Linq;
using csShared.FloatingElements;
using csShared.Geo;
using csShared.Interfaces;

namespace csCommon.MapPlugins.FindLocation
{
    using System.ComponentModel.Composition;

    [Export(typeof(IPlugin))]
    public class FindLocationPlugin : PropertyChangedBase,IPlugin
    {

        public bool CanStop { get { return true; } }

        private ISettingsScreen _settings;

        public ISettingsScreen Settings
        {
            get { return _settings; }
            set { _settings = value; NotifyOfPropertyChange(() => Settings); }
        }

        private IPluginScreen _screen;

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
            get { return 4; }
        }

        public string Icon
        {
            get { return @"icons\search.png"; }
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
            get { return "FindLocation"; }
        }

        public void Init()
        {
            var viewModel = IoC.GetAllInstances(typeof (IFindLocation)).FirstOrDefault();
            if (viewModel != null)
            {                
                Element = FloatingHelpers.CreateFloatingElement("Find", DockingStyles.Up, viewModel,Icon,Priority);
                UpdateVisibility();
            }
            AppState.ViewDef.VisibleChanged += ViewDefVisibleChanged;
        }

        void ViewDefVisibleChanged(object sender, VisibleChangedEventArgs e)
        {
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (AppState.ViewDef.Visible && IsRunning)
            {
                if (!AppState.FloatingItems.Contains(Element)) AppState.FloatingItems.AddFloatingElement(Element);
            }
            else
            {
                if (AppState.FloatingItems.Contains(Element)) AppState.FloatingItems.RemoveFloatingElement(Element);
            }
        }


        public void Start()
        {
            IsRunning = true;
            UpdateVisibility();
        }

        public void Pause()
        {
            IsRunning = false;
            UpdateVisibility();
        }

        public void Stop()
        {
            IsRunning = false;
            UpdateVisibility();
        }
    }
}
