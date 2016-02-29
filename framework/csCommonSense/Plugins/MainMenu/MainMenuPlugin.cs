using System;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using ESRI.ArcGIS.Client;
using csShared;
using csShared.Controls.SlideTab;
using csShared.Interfaces;
using System.ComponentModel.Composition;
using csShared.TabItems;

namespace csCommon.MapPlugins.MainMenu
{
    [Export(typeof(IPlugin))]
    public class MainMenuPlugin : PropertyChangedBase, IPlugin
    {
        public bool CanStop { get { return false; } }

        private ISettingsScreen _settings;

        public ISettingsScreen Settings
        {
            get { return _settings; }
            set { _settings = value; NotifyOfPropertyChange(() => Settings); }
        }
        private GroupLayer _layer = new GroupLayer() { ID = "MainMenu" };

        private IPluginScreen _screen;

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

        private StartPanelTabItem _tabItem;

        private bool _isRunning;

        public bool IsRunning
        {
            get { return _isRunning; }
            set { _isRunning = value; NotifyOfPropertyChange(() => IsRunning); }
        }

        public AppStateSettings AppState { get; set; }

        public FloatingElement Element { get; set; }

        private void CheckLayer()
        {
            if (!AppState.ViewDef.Layers.ChildLayers.Contains(_layer))
            {
                AppState.ViewDef.Layers.ChildLayers.Add(_layer);
                _layer.Initialize();
            }
        }

        
        public string Name
        {
            get { return "MainMenu"; }
        }

        public void Init()
        {           
            _tabItem = new StartPanelTabItem();
            _tabItem.ModelInstance = new MainMenuViewModel();
            _tabItem.Name = "Main Menu";
            _tabItem.HeaderStyle = TabHeaderStyle.Image;
            _tabItem.Image = new BitmapImage(new Uri("pack://application:,,,/csCommon;component/Resources/Icons/Home.png"));
            
            AppState.AddStartPanelTabItem(_tabItem);
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
