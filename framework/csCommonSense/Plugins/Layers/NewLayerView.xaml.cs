using System.Linq;
using System.Windows;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Toolkit.DataSources;
using csShared;
using csShared.Geo;

namespace csGeoLayers.Plugins.Layers
{
    public partial class NewLayerView
    {
        public NewLayerView()
        {
            InitializeComponent();
        }

        private void SbAddClick(object sender, RoutedEventArgs e)
        {
            var l = ((FrameworkElement) sender).DataContext as StoredLayer;
            if (l == null) return;

            switch (l.Type)
            {
                case "Map Service":
                    var ml = new ArcGISImageServiceLayer();
                    

                    ml.Url = l.Id;
                    ml.ID = l.Title;
                    ml.Visible = true;
                    
                    var pts = AppStateSettings.Instance.ViewDef.FindOrCreateAcceleratedGroupLayer(l.Path);
                    pts.ChildLayers.Add(ml);
                    ml.Initialize();
                    
                    break;
                // FIXME TODO: Unreachable code
                    //break;
                case "Feature Service":
                    var fl = new FeatureLayer() {};

                    fl.Url = l.Id + @"/0";
                    fl.ID = l.Title;
                    fl.Visible = true;
                    fl.InitializationFailed += fl_InitializationFailed;
                    fl.Initialized += fl_Initialized;
                    fl.UpdateCompleted += fl_UpdateCompleted;
                    var pt = AppStateSettings.Instance.ViewDef.FindOrCreateAcceleratedGroupLayer(l.Path);
                    pt.ChildLayers.Add(fl);
                    fl.Initialize();
                    fl.Update();
                    break;
                case "wms":
                    var wl = new WmsLayer {
                                              SupportedSpatialReferenceIDs = new[] {102100},
                                              Visible = false
                                          };
                    wl.Visible = true;
                    wl.SkipGetCapabilities = false;
                    wl.Initialized += (st, es) => { wl.Layers = wl.LayerList.Select(k => k.Title).ToArray(); };
                    wl.Url = l.Id;
                    wl.ID = l.Title;
                    wl.Title = l.Title;

                    var p = AppStateSettings.Instance.ViewDef.FindOrCreateGroupLayer(l.Path);
                    p.ChildLayers.Add(wl);
                    wl.Initialize();

                    break;
            }
            AppStateSettings.Instance.ViewDef.StoredLayers.Add(l);
            AppStateSettings.Instance.ViewDef.StoredLayers.Save();
        }

        void fl_UpdateCompleted(object sender, System.EventArgs e)
        {
            
        }

        void fl_Initialized(object sender, System.EventArgs e)
        {
            
        }

        void fl_InitializationFailed(object sender, System.EventArgs e)
        {
            
        }
    }
}