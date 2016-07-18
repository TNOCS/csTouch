using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using csCommon.csMapCustomControls.CircularMenu;
using csCommon.MapPlugins.EsriMap;
using csShared;
using csShared.Interfaces;
using Caliburn.Micro;

namespace csCommon.Plugins.MgrsGrid
{
    [Export(typeof(IPlugin))]
    public class MgrsGridPlugin : PropertyChangedBase, IPlugin
    {
        // EsriDynamicMap => Best option: rendered as ESRI map
        // Overlay ==> Rendered as overlay on map; safed option
        // Deug ==> Both on, should match!
        public enum ERenderMethode {  EsriDynamicMap, Overlay, Debug };

        public const ERenderMethode RenderMethode = ERenderMethode.EsriDynamicMap;

        public const string PluginName = "MGRS Plugin";
        private CircularMenuItem mNavigatorMenu;
        private MgrsRasterLayer mNonEsriLayer;
        private MgrsCoordinateLabel mCoordinateOverlay;
        public MgrsGridPlugin()
        {

        }


        public string Name
        {
            get { return PluginName; }
        }

        public csShared.AppStateSettings AppState
        {
            get
            {
                return AppStateSettings.Instance;
            }
            set
            {
                // Do nothing
            }
        }

        private bool isRunning;
        public bool IsRunning
        {
            get { return isRunning; }
            set { isRunning = value; NotifyOfPropertyChange(() => IsRunning); }
        }

        public string Icon
        {
            get { return @"/csCommon;component/Resources/Icons/SearchIcon.png"; }
        }

        public int Priority
        {
            get { return 6; }
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

        public bool CanStop
        {
            get { return true; }
        }


        public void Init()
        {
            Cfg = new MgrsConfig();
            var esriMap = AppStateSettings.Instance.Plugins.FirstOrDefault(x => x is EsriMapPlugin)?.Screen as EsriMapViewModel;
            if (esriMap != null)
            {
                var grid100mMenu = new CircularMenuItem() { Title = "100 m", Position = 1 };
                var grid1kmMenu = new CircularMenuItem() { Title = "1 km", Position = 2 };
                var grid10kmMenu = new CircularMenuItem() { Title = "10 km", Position = 3 };
                var grid100kmMenu = new CircularMenuItem() { Title = "100 km", Position = 4 };
                var toggleMenu = new CircularMenuItem() { Title = "toggle", Position = 5 };
                var centerLabelMenu = new CircularMenuItem() { Title = "label", Position = 6 };
                var defaultMenu = new CircularMenuItem() { Title = "default", Position = 7 };
                mNavigatorMenu = new CircularMenuItem()
                {
                    Icon = @"/csCommon;component/Resources/Icons/MgrsIcon.png",
                    Title = "MGRS",
                    Position = 8,
                    Items = new List<CircularMenuItem>()
                    {
                        grid100mMenu,
                        grid1kmMenu,
                        grid10kmMenu,
                        grid100kmMenu,
                        toggleMenu,
                        centerLabelMenu,
                        defaultMenu
                     }
                };






                toggleMenu.Selected += (sender, args) => ToggleMenu();
                centerLabelMenu.Selected += (sender, args) => MenuCenterLabel();
                defaultMenu.Selected += (sender, args) => MenuDefaultSettings();
                grid100mMenu.Selected += (sender, args) => SetGridSize(DrawMgrsRaster.EGridPrecision.Grid100m);
                grid1kmMenu.Selected += (sender, args) => SetGridSize(DrawMgrsRaster.EGridPrecision.Grid1km);
                grid10kmMenu.Selected += (sender, args) => SetGridSize(DrawMgrsRaster.EGridPrecision.Grid10km);
                grid100kmMenu.Selected += (sender, args) => SetGridSize(DrawMgrsRaster.EGridPrecision.Grid100km);
                switch(RenderMethode)
                {
                    case ERenderMethode.EsriDynamicMap:
                        mNonEsriLayer = null;
                        mEsriLayer = new MgrsEsriLayer(Cfg);
                        break;
                    case ERenderMethode.Overlay:
                        mNonEsriLayer = new MgrsRasterLayer(Cfg);
                        mEsriLayer = null;
                        break;
                    case ERenderMethode.Debug:
                        mNonEsriLayer = new MgrsRasterLayer(Cfg);
                        mEsriLayer = new MgrsEsriLayer(Cfg);
                        break;

                }
                if (mNonEsriLayer != null) esriMap.AddMapOverlay(mNonEsriLayer);
                if (mEsriLayer != null) esriMap.AddEsriLayer(mEsriLayer);
                mCoordinateOverlay = new MgrsCoordinateLabel();
                esriMap.AddMapOverlay(mCoordinateOverlay);
            }
        }

        private MgrsEsriLayer mEsriLayer;

        private void SetGridSize(DrawMgrsRaster.EGridPrecision pPrecision)
        {
            double meterperpixel = LastMetersPerPixel;
            if (meterperpixel != 0)
            {
                Cfg.SetLevel(pPrecision, meterperpixel);
                if (!Cfg.IsEnabled) Cfg.IsEnabled = true;
                RefreshLayer();
            }
        }

        private void MenuDefaultSettings()
        {
            Cfg.SetDefaultLevels();
        }


        private void RefreshLayer()
        {
            if (mNonEsriLayer != null) mNonEsriLayer.InvalidateVisual();
            if (mEsriLayer != null) mEsriLayer.Refresh();
        }

        private double LastMetersPerPixel
        {
            get
            {
                switch(RenderMethode)
                {
                    case ERenderMethode.EsriDynamicMap:
                        return mEsriLayer?.LastMetersPerPixel ?? 0;
                    case ERenderMethode.Overlay:
                        return mNonEsriLayer?.LastMetersPerPixel ?? 0;
                    case ERenderMethode.Debug:
                        return mNonEsriLayer?.LastMetersPerPixel ?? 0;

                }
                return 0;
            }
        }



        private void ToggleMenu()
        {
            Cfg.IsEnabled = !Cfg.IsEnabled;
            RefreshLayer();
        }

        private void MenuCenterLabel()
        {
            Cfg.DrawCenterMgrsGridLabel = !Cfg.DrawCenterMgrsGridLabel;
            RefreshLayer();
        }
        

        public void Start()
        {
            IsRunning = true;
            if (mNavigatorMenu != null)
               AppStateSettings.Instance.CircularMenus.Add(mNavigatorMenu);
        }

        

        public void Pause()
        {

        }

        public void Stop()
        {
            IsRunning = false;
            if (mNavigatorMenu != null)
                AppStateSettings.Instance.CircularMenus.Remove(mNavigatorMenu);
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;


        private ISettingsScreen settings;

        public ISettingsScreen Settings
        {
            get { return settings; }
            set { settings = value; NotifyOfPropertyChange(() => Settings); }
        }

        public MgrsConfig Cfg { get; private set; }
    }
}
