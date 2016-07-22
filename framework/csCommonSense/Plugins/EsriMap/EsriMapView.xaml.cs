//using csGeoLayers.Properties;
using csShared;
using csShared.Geo;
using csShared.Geo.Esri;
using csShared.Utils;
using ESRI.ArcGIS.Client;
using Microsoft.Surface.Presentation;
using SharpMap.Geometries;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using TransitionEffects;
using ITileImageProvider = csShared.Geo.Esri.ITileImageProvider;
using Point = System.Windows.Point;

namespace csCommon.MapPlugins.EsriMap
{
    /// <summary>
    /// Interaction logic for EsriMapView.xaml
    /// </summary>
    public partial class EsriMapView
    {
        private readonly AppStateSettings appState = AppStateSettings.Instance;
        private readonly Canvas geoLayerCanvas = new Canvas();
        private readonly MapViewDef viewDef = AppStateSettings.Instance.ViewDef;

        public EsriMapView()
        {
            InitializeComponent();
            Loaded += EsriMapViewLoaded;
            appState.ViewDef.SetMapControl(emMain);
            appState.ViewDef.BaseLayer = wtlBase;
            appState.ViewDef.Layers = glAppLayers;
            appState.ViewDef.BaseLayers = glBaseLayers;
            appState.ViewDef.AcceleratedLayers = glAcceleratedLayers;
            //appState.ViewDef.AccBaseLayer = wtlAccBase;
            appState.ViewDef.MapToolsLayer = glMapTools;
            appState.ViewDef.RootLayer = lcMain;
        }

        public void StartTransition()
        {
            var width = emMain.ActualWidth;
            var height = emMain.ActualHeight;
            if (!(width > 0) || !(height > 0)) return;
            iTransition.Visibility = Visibility.Visible;
            var bmpCopied = new RenderTargetBitmap((int)Math.Round(width), (int)Math.Round(height), 96, 96, PixelFormats.Default);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                var vb = new VisualBrush(emMain);
                dc.DrawRectangle(vb, null, new Rect(new Point(), new Size(width, height)));
            }
            bmpCopied.Render(dv);

            iTransition.Source = bmpCopied;
            emMain.Visibility = Visibility.Collapsed;
        }


        private void EsriMapViewLoaded(object sender, RoutedEventArgs e)
        {
            appState.ViewDef.TransitionStarted += ViewDefTransitionStarted;

            //AppState.ViewDef.Layers.ChildLayers.Add(AppState.ViewDef.MapTools);
            emMain.WrapAround = true;
            emMain.ZoomDuration = new TimeSpan(0, 0, 0, 0, 300);
            emMain.PanDuration = new TimeSpan(0, 0, 0, 0, 300);

            //wtlBase.TileProvider = appState.ViewDef.SelectedBaseLayer;
            

            emMain.ExtentChanged += EmMainExtentChanged;
            emMain.ExtentChanging += EmMainExtentChanging;


            if (!canvas.Children.Contains(geoLayerCanvas))
                canvas.Children.Add(geoLayerCanvas);

            

            var bl = appState.Config.Get("Map.Type", "");
            if (string.IsNullOrEmpty(bl) && appState.ViewDef.BaseLayerProviders.Any())
            {
                bl = appState.ViewDef.BaseLayerProviders.First().Title;
                appState.ViewDef.ChangeMapType(bl);
            }
             
            



            emMain.SetBinding(Map.RotationProperty, new Binding("Rotation") { Source = appState.ViewDef });


            //var ns = new KmlLayer {
            //                          Url = new Uri("http://openov.nl/haltes/data/spoorkaart.kml")
            //                      };
            //lcMain.Add(ns);

            //var sd = new ShapeDisplay(Application.Current.MainWindow, canvasShapes);
            //sd.GeometryAdd += SdGeometryAdd; // Hierin worden de lijnen van de shapes geplot
            //sd.ReadShapeFile(@"C:\temp\spoorkaart\polygons6.shp"); // Hier wordt de locale shape geladen. 

            //var wl = new WmtsLayer {
            //                           Url = "http://cool.telecom.tno.nl:8004/geoserver/gwc/service/wmts",
            //                           Version = "1.0.0",
            //                           ServiceMode = WmtsLayer.WmtsServiceMode.KVP,
            //                           TileMatrixSet = "EPSG:900913",
            //                           ID = "test",
            //                           ImageFormat = "image/png",
            //                           Layer = "cdag4stars:risicokaart1"
            //                       };

            //lcMain.Add(wl);

            //var wl2 = new WmsLayer {
            //                           Url = "http://cool.telecom.tno.nl:8004/geoserver/gwc/",
            //                           Version = "1.1.0",
            //                           Layers = new[] {"cdag4stars:postcodevlakkenvenlo"}
            //                       };
            //wl2.Url = "http://cool.telecom.tno.nl:8004/geoserver/cdag4stars/wms?service=WMS";
        }

        void ViewDefTransitionStarted(object sender, EventArgs e)
        {
            StartTransition();
            emMain.ExtentChanged += emMain_ExtentChanged;
        }

        




        private void EmMainExtentChanging(object sender, ExtentEventArgs e)
        {

            viewDef.Extent = new BoundingBox(e.NewExtent.XMin, e.NewExtent.YMin, e.NewExtent.XMax, e.NewExtent.YMax);
            viewDef.UpdateWorldExtent();
            viewDef.RaiseManipulationDelta();
            //_invalid = true;
        }

        private void EmMainExtentChanged(object sender, ExtentEventArgs e)
        {
            try
            {

                viewDef.UpdateWorldExtent();
                viewDef.RaiseManipulationCompleted();
            }
            catch (Exception ex)
            {
                Logger.Log("Esri Map","Error updating map extent",ex.Message,Logger.Level.Error);
            }
        }


        private void GridDrop(object sender, SurfaceDragDropEventArgs e)
        {
            AppStateSettings.Instance.StartDrop(new DropEventArgs
                                                    {
                                                        Pos = e.Cursor.GetPosition(this),
                                                        Orientation = e.Cursor.GetOrientation(this),
                                                        EventArgs = e
                                                    });
        }


        void emMain_ExtentChanged(object sender, ExtentEventArgs e)
        {

            //iTransition.Visibility = Visibility.Collapsed;
            emMain.Visibility = Visibility.Visible;

            var rte = new RadialWiggleTransitionEffect();
            var da = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromSeconds(1.5)), FillBehavior.HoldEnd)
            {
                AccelerationRatio = 0.5,
                DecelerationRatio = 0.5
            };
            da.Completed += da_Completed;
            //da.Completed += new EventHandler(this.TransitionCompleted);
            var vb = new VisualBrush(iTransition)
            {
                Viewbox = new Rect(0, 0, iTransition.ActualWidth, iTransition.ActualHeight),
                ViewboxUnits = BrushMappingMode.Absolute
            };
            //this.oldChild.Width = this.oldChild.ActualWidth;
            //this.oldChild.Height = this.oldChild.ActualHeight;
            //this.oldChild.Measure(new Size(this.oldChild.ActualWidth, this.oldChild.ActualHeight));
            //this.oldChild.Arrange(new Rect(0, 0, this.oldChild.ActualWidth, this.oldChild.ActualHeight));

            rte.OldImage = vb;
            emMain.Effect = rte;
            rte.BeginAnimation(TransitionEffect.ProgressProperty, da);
            emMain.ExtentChanged -= emMain_ExtentChanged;
        }


        void da_Completed(object sender, EventArgs e)
        {
            emMain.Effect = null;
            iTransition.Visibility = Visibility.Collapsed;
        }
    }
}