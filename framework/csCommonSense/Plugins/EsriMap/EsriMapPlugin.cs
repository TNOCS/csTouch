using System;
using System.Globalization;
using System.IO;
using Caliburn.Micro;
using csShared.Geo.Esri;
using csShared.Utils;
using DataServer;
using ESRI.ArcGIS.Client;
using csShared;
using csShared.Interfaces;
using System.ComponentModel.Composition;
using ESRI.ArcGIS.Client.Geometry;

namespace csCommon.MapPlugins.EsriMap
{
    [Export(typeof(IPlugin))]
    public class EsriMapPlugin : PropertyChangedBase, IPlugin
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
            get { return @"/csCommon;component/Resources/Icons/globe.png"; }
        }

        private bool isRunning;

        public bool IsRunning
        {
            get { return isRunning; }
            set { isRunning = value; NotifyOfPropertyChange(() => IsRunning); }
        }

        public AppStateSettings AppState { get; set; }


        public string Name
        {
            get { return "EsriMap"; }
        }

        private const string BasemapState = "basemap";
        private const string ExtentState = "mapextent";
        private const string RotationState = "rotation";

        public void SetDashboard(ref DashboardState dashboard)
        {
            var r = "";
            foreach (WebTileLayer bl in AppState.ViewDef.BaseLayers)
            {
                r += bl.TileProvider.Title + "|" + bl.TileProvider.MBTileFile + "|" + bl.Opacity.ToString(CultureInfo.InvariantCulture) + ";";
            }
            
            dashboard.States[BasemapState] = r;
            dashboard.States[ExtentState] = new EnvelopeConverter().ConvertToString(AppState.ViewDef.MapControl.Extent.ToString());
            dashboard.States[RotationState] = AppState.ViewDef.MapControl.Rotation.ToString(CultureInfo.InvariantCulture);
        }

        void Dashboards_DashboardActivated(object sender, DashboardEventArgs e)
        {
            if (e.Dashboard == null) return;
            var s = e.Dashboard.States;
            if (s.ContainsKey(BasemapState))
            {
                AppState.ViewDef.BaseLayers.ChildLayers.Clear();
                var ll = s[BasemapState].Split(';');
                foreach (var l in ll)
                {
                    var pp = l.Split('|');
                    if (pp.Length == 3)
                    {
                       
                            AppState.ViewDef.AddMapType(pp[0], double.Parse(pp[2], CultureInfo.InvariantCulture));
                        
                    }
                }
                
            }
            if (s.ContainsKey(RotationState))
            {
                AppState.ViewDef.MapControl.Rotation = double.Parse(s[RotationState], CultureInfo.InvariantCulture);
            }
            if (!s.ContainsKey(ExtentState)) return;
            //AppState.ViewDef.StartTransition();
            var ex = s[ExtentState];
            AppState.ViewDef.MapControl.Extent = (Envelope)new EnvelopeConverter().ConvertFromString(ex);
        }

        public void Init() {
            if (!ArcGISRuntime.IsInitialized) {
                try {
                    ArcGISRuntime.Initialize();
                }
                catch (Exception e) {
                    Logger.Log("Esri License", "Not valid, please contact administrator", "", Logger.Level.Fatal, true);
                    throw e;
                }
            }
            Screen = new EsriMapViewModel();
            AppState.DashboardStateStates.RegisterHandler(SetDashboard);
            AppState.DashboardStateStates.DashboardActivated += Dashboards_DashboardActivated;

            AddMBTileLayers();
        }

        private string mbtileFolder;

        public void AddMBTileLayers()
        {
            mbtileFolder = AppState.Config.Get("MBTileFolder", "MBTileLayers");
            if (mbtileFolder[mbtileFolder.Length - 1] != '\\') mbtileFolder += @"\";
            if (!Directory.Exists(mbtileFolder)) return;
            var dir = new DirectoryInfo(mbtileFolder);
            foreach (var f in dir.GetFiles("*.mbtiles"))
            {
                AppState.ViewDef.AddMBTileLayer(f.FullName);
            }
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
            if (AppState.ViewDef.MapControl!=null) 
                AppState.Config.SetLocalConfig("Map.Extent", new EnvelopeConverter().ConvertToString(AppState.ViewDef.MapControl.Extent), true);
            IsRunning = false;
        }
    }
}
