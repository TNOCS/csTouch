using System;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using csCommon.MapPlugins.EsriMap;
using csShared;
using csShared.Controls.SlideTab;
using csShared.Geo;
using csShared.Interfaces;
using System.ComponentModel.Composition;
using csShared.TabItems;

namespace csCommon.MapPlugins.MapSelection
{
    [Export(typeof(IPlugin))]
    public class MapsPlugin : PropertyChangedBase, IPlugin
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
            get { return @"/csCommon;component/Resources/Icons/map-white.png"; }
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
            get { return "MapSelectionPlugin"; }
        }

        private StartPanelTabItem spti;

        public void Init()
        {
            var viewModel = new MapSelectionViewModel();
            spti = new StartPanelTabItem
            {
                ModelInstance = viewModel,
                Position      = StartPanelPosition.left,
                HeaderStyle   = TabHeaderStyle.Image,
                Name          = "Map Styles",
                Image         = new BitmapImage(new Uri(@"pack://application:,,,/csCommon;component/Resources/Icons/map-white.png", UriKind.RelativeOrAbsolute))
            };

            AppState.ConfigTabs.Add(new EsriMapSettingsViewModel { DisplayName = "Map"});

            //if (viewModel != null)
            //{                
            //    Element = FloatingHelpers.CreateFloatingElement(viewModel.Caption, DockingStyles.Up, viewModel, Icon, Priority);
            //    Element.ModelInstanceBack = new EsriMapSettingsViewModel();
            //    Element.CanFlip = true;
            //    Element.StartSize = new Size(75, 75);                
                
            //    Element.LastContainerPosition = new ContainerPosition()
            //    {
            //        Center = new Point(700, 400),
            //        Size = new Size(550, 550)

            //    };
            //    Element.SwitchWidth = 500;
            //    Element.DragScaleFactor = 40;
            //    UpdateVisibility();
            //}
            AppState.ViewDef.VisibleChanged += ViewDefVisibleChanged;
            AppState.InitMapShortcuts();
            AppState.ViewDef.SelectedBaseLayerChanged += (e, s) => AppState.CheckMapShortcuts();
        }

        void ViewDefVisibleChanged(object sender, VisibleChangedEventArgs e)
        {
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (AppState.ViewDef.Visible && IsRunning)
            {
                AppState.AddStartPanelTabItem(spti);
            }
            else
            {
                AppState.RemoveStartPanelTabItem(spti);
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
