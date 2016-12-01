using System;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using ESRI.ArcGIS.Client.Toolkit.DataSources;
using csShared;
using System.Linq;
using csShared.Controls.SlideTab;
using csShared.Geo;
using csShared.Interfaces;
using csShared.TabItems;


namespace csCommon
{
    using System.ComponentModel.Composition;

    [Export(typeof(IPlugin))]
    public class LayersPlugin :  PropertyChangedBase,IPlugin
    {
        public const string PluginName = "Layers";

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
            get { return 2; }
        }

        public string Icon
        {
            get { return @"/csCommon;component/Resources/Icons/layers3.png"; }
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
            get { return PluginName; }
        }

        public LayersViewModel LayersVM { get; private set; }

        public void Init()
        {
            LayersVM = new LayersViewModel(); // IoC.GetAllInstances(typeof(ILayerSelection)).Where(k => ((ILayerSelection)k).Caption == "Layers").FirstOrDefault() as IMapSelection;


            spti = new StartPanelTabItem
            {
                ModelInstance = LayersVM,
                Position = StartPanelPosition.left,
                HeaderStyle = TabHeaderStyle.Image,
                Name = "Layers",
                Image = new BitmapImage(new Uri(@"pack://application:,,,/csCommon;component/Resources/Icons/layers3.png", UriKind.RelativeOrAbsolute))
            };


            //if (!AppState.LeftTabMenuVisible)
            //{
            //    Element = FloatingHelpers.CreateFloatingElement(viewModel.Caption, DockingStyles.Right, viewModel, Icon, Priority);
            //    Element.ModelInstanceBack = new NewLayerViewModel();
            //    Element.CanFlip = true;
            //    Element.CanScale = true;
            //    //Element.StartSize = new Size(75, 100);
            //    Element.SwitchWidth = 600;
            //    ////Element.StartSize = new Size(50,50);
            //    AppState.FloatingItems.Add(Element);
            //}
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

        private StartPanelTabItem spti;

        public void Start()
        {
            IsRunning = true;
            UpdateVisibility();
            AppState.ViewDef.VisibleChanged += ViewDefVisibleChanged;

            // find layers

            LoadStoredLayers();
                       
        }

        private void LoadStoredLayers()
        {
            // FIXME TODO: Unreachable code
//            return;
//            AppState.ViewDef.StoredLayers.Load();
//            foreach (var s in AppState.ViewDef.StoredLayers)
//            {
//                if (s.Type != "wms") continue;
//                var wl = new WmsLayer
//                {
//                    SupportedSpatialReferenceIDs = new[] {102100},
//                    Visible = true,
//                    Url = s.Id,
//                    Title = s.Title,
//                    ID = s.Title,
//                    SkipGetCapabilities = false
//                };
//
//                wl.Initialized += (st, es) => { wl.Layers = wl.LayerList.Select(k => k.Title).ToArray(); };
//                wl.SetValue(LayerExtensions.StoredProperty, s);
//
//                //wl.Layers = new string[] { "inspireadressen" };
//                var gl = AppState.ViewDef.FindOrCreateGroupLayer(s.Path);
//                gl.ChildLayers.Add(wl);
//                wl.Initialize();
//            }
        }

        public void Pause()
        {
         
        }

        public void Stop()
        {
            IsRunning = false;
            UpdateVisibility();
            
        }

        
    }
}
