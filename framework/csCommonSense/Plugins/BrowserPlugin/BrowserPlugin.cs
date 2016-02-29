using System;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using ESRI.ArcGIS.Client;
using csShared;
using csShared.Controls.SlideTab;
using csShared.Interfaces;
using System.ComponentModel.Composition;
using csShared.TabItems;

namespace csCommon.Plugins.BrowserPlugin
{
    [Export(typeof(IPlugin))]
    public class BrowserPlugin : PropertyChangedBase, IPlugin
    {
        public bool CanStop { get { return false; } }

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
            get { return @"icons\compass.png"; }
        }

        private StartPanelTabItem tabItem;

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
            get { return "BrowserPlugin"; }
        }

        public void Init()
        {           
            tabItem = new StartPanelTabItem();
            tabItem.ModelInstance = new BrowserViewModel();
            tabItem.Name = "Web";
            tabItem.HeaderStyle = TabHeaderStyle.Image;
            tabItem.Image = new BitmapImage(new Uri("pack://application:,,,/csCommon;component/Resources/Icons/ie.png"));
            tabItem.Position = StartPanelPosition.right;
            AppState.AddStartPanelTabItem(tabItem);
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
