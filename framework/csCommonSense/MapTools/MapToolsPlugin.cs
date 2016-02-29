using System;
using System.ComponentModel.Composition;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using csShared;
using csShared.Controls.SlideTab;
using csShared.Geo;
using csShared.Interfaces;
using csShared.TabItems;

namespace csCommon.MapPlugins.MapTools
{
    [Export(typeof (IPlugin))]
    public class MapToolsPlugin : PropertyChangedBase, IPlugin
    {
        private bool hideFromSettings;
        private bool isRunning;
        private IPluginScreen screen;
        private ISettingsScreen settings;
        private StartPanelTabItem tabItem;
        

        public bool CanStop
        {
            get { return true; }
        }

        public ISettingsScreen Settings
        {
            get { return settings; }
            set
            {
                settings = value;
                NotifyOfPropertyChange(() => Settings);
            }
        }

        public IPluginScreen Screen
        {
            get { return screen; }
            set
            {
                screen = value;
                NotifyOfPropertyChange(() => Screen);
            }
        }

        public bool HideFromSettings
        {
            get { return hideFromSettings; }
            set
            {
                hideFromSettings = value;
                NotifyOfPropertyChange(() => HideFromSettings);
            }
        }

        public int Priority
        {
            get { return 1; }
        }

        public string Icon
        {
            get { return @"/csCommon;component/Resources/Icons/tools2.png"; }
        }

        public bool IsRunning
        {
            get { return isRunning; }
            set
            {
                isRunning = value;
                NotifyOfPropertyChange(() => IsRunning);
            }
        }

        public AppStateSettings AppState { get; set; }


        public string Name
        {
            get { return "MapTools"; }
        }

        public void Init()
        {
            // MapToolsViewModel mtV = (MapToolsViewModel)IoC.GetInstance(typeof (MapToolsViewModel),"");

            

            //CompassViewModel viewModel = new CompassViewModel();
            //if (viewModel != null)
            //{
            //    viewModel.AppState = AppState;
            //    Element = FloatingHelpers.CreateFloatingElement("Compass",DockingStyles.Right, viewModel, Icon, Priority);
            //    viewModel.ToolLayer = this.Layer;
            //    //Element.ShowHeader = false;
            //    //Element.CanExit = true;
            //    Element.Style = Application.Current.FindResource("SimpleContainer") as Style;
            //    Element.SwitchWidth = 175;
            //    Element.CanRotate = false;

            //    Element.CanScale = false;
            //    //Element.MaxSize = new Size(400, 175);
            //    Element.StartSize = new Size(150, 75);
            //    //Element.Width = 400;
            //    //Element.Height = 400;true


            //    Element.SwitchWidth = 400;

            //    UpdateVisibility();
            //}
            AppState.ViewDef.VisibleChanged += ViewDefVisibleChanged;
        }


        public void Start()
        {
            IsRunning = true;
            tabItem = new StartPanelTabItem
            {
                ModelInstance = new MapToolsViewModel(),
                Name = "Map Tools",
                HeaderStyle = TabHeaderStyle.Image,
                Image = new BitmapImage(new Uri("pack://application:,,,/csCommon;component/Resources/Icons/tools2.png"))
            };

            AppState.AddStartPanelTabItem(tabItem);

        }

        public void Pause()
        {
            IsRunning = false;
            
        }

        public void Stop()
        {
            IsRunning = false;
            AppState.RemoveStartPanelTabItem(tabItem);
        }

        private void ViewDefVisibleChanged(object sender, VisibleChangedEventArgs e)
        {
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            //if (IsRunning) //AppState.ViewDef.Visible && 
            //{
            //    if (!AppState.FloatingItems.Contains(Element) && Element != null) AppState.FloatingItems.AddFloatingElement(Element);
            //}
            //else
            //{
            //    AppState.FloatingItems.RemoveFloatingElement(Element);
            //}
        }
    }
}