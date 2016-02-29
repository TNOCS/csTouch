using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using ESRI.ArcGIS.Client.Symbols;
using csPresenterPlugin.Controls;
using csShared;

namespace csPresenterPlugin.Layers
{
    [Serializable]
    public class PresenterLayer : GroupLayer
    {
        private readonly WebMercator _mercator = new WebMercator();
        //public event EventHandler Loaded;

        private MetroExplorer _explorer;

        public MetroExplorer Explorer
        {
            get { return _explorer; }
            set { _explorer = value; }
        }

        private GraphicsLayer _graphics;

        public GraphicsLayer Graphics
        {
            get { return _graphics; }
            set { _graphics = value;                
            }
        }


        private string layerFile;

        public string LayerFile
        {
            get { return layerFile; }
            set { layerFile = value; }
        }
        

        public override void Initialize()
        {
            if (LayerFile == null || !File.Exists(LayerFile)) return;
            var fileInfo = new FileInfo(LayerFile);
            if (fileInfo.Directory != null) ID = fileInfo.Directory.Name;

            Graphics = new GraphicsLayer();
            ChildLayers.Add(Graphics);
            Graphics.Initialize();

            ID = "Presenter Layer";
            base.Initialize();
            SpatialReference = new SpatialReference(4326);
            ChildLayers.Insert(0, el);
            UpdateLayer();
        }

        private FileInfo fi;
        private readonly ElementLayer el = new ElementLayer();
        private Envelope envelope;
        //private bool _fullLayer = false;

        private void UpdateLayer()
        {
            
            fi = new FileInfo(LayerFile);

            ImageSource ims = null;
            var defaultImage = fi.Directory + @"\_" + fi.Name.Substring(0,fi.Name.Length-4) + ".png";
            if (!File.Exists(defaultImage)) {
                defaultImage = "";
            }
            else {
                ims = new BitmapImage(new Uri(defaultImage));
            }

            double iconsize = 30;
            el.Children.Clear();

            Explorer.HasFullLayer = false;
            foreach (var a in File.ReadAllLines(LayerFile))
            {
                if (a.StartsWith("~"))
                {
                    var s = a.Remove(0, 1);
                    var ss = s.Split(',');
                    if (ss.Length <= 0) continue;
                    switch (ss[0])
                    {
                        case "iconsize":
                            iconsize = Convert.ToDouble(ss[1]);
                            break;
                        case "fulllayer":
                            try {                                    
                                Explorer.HasFullLayer = true;
                                var image = ss[1];
                                var width = Convert.ToDouble(ss[2], CultureInfo.InvariantCulture);
                                var height = Convert.ToDouble(ss[3], CultureInfo.InvariantCulture);

                                var u = new Uri(fi.Directory + "/" + image);
                                var i = new Image {
                                    Source = new BitmapImage(u),
                                    Width = double.NaN,
                                    Height = double.NaN,
                                    HorizontalAlignment = HorizontalAlignment.Stretch,
                                    VerticalAlignment = VerticalAlignment.Stretch,
                                    Stretch = Stretch.Fill
                                };
                                el.Children.Add(i);

                                var w = new WebMercator();
                                var mpa = w.FromGeographic(new MapPoint(0, 0, new SpatialReference(4326))) as MapPoint;
                                var mpb = w.FromGeographic(new MapPoint(width, height, new SpatialReference(4326))) as MapPoint;
                                envelope = new Envelope(mpa, mpb);
                                ElementLayer.SetEnvelope(i, envelope);

                                //this.ChildLayers.Add(el);

                                i.MouseDown += i_PreviewMouseDown;
                                i.PreviewMouseWheel += i_PreviewMouseWheel;
                                i.PreviewTouchDown += i_PreviewTouchDown;
                                el.Initialize();
                                AppStateSettings.Instance.ViewDef.MapControl.Extent = envelope;
                                //AppStateSettings.Instance.ViewDef.CanMove = false;
                                AppStateSettings.Instance.ViewDef.BaseLayer.Opacity = 0;
                                break;
                            }
                            catch (SystemException e) {
                                break;
                            }
                    }
                }
                else
                {
                    try {
                        var l1 = a.IndexOf(',');
                        var lon = Convert.ToDouble(a.Substring(0, l1), CultureInfo.InvariantCulture);
                        var l2 = a.IndexOf(',', l1 + 1);
                        var lat = Convert.ToDouble(a.Substring(l1 + 1, l2 - l1 - 1), CultureInfo.InvariantCulture);
                        var l3 = a.IndexOf(',', l2 + 1);
                        var path = a.Substring(l2 + 1, l3 - l2 - 1);
                        //var l4 = a.IndexOf(',', l3 + 1);
                        //var image = a.Substring(l3 + 1, l4 - l3 - 1);
                        //if (string.IsNullOrEmpty(image)) image = defaultImage;

                        var title = a.Substring(l3 + 1).Trim('"');

                        path = fi.Directory.FullName + @"\" + path;

                        var g = new Graphic();
                        g.Attributes["id"]        = title;
                        g.Attributes["path"]      = path;
                        g.Attributes["size"]      = iconsize;
                        g.Attributes["explorer"]  = Explorer;
                        g.Attributes["image"]     = ims;
                        g.Attributes["fulllayer"] = Explorer.HasFullLayer;

                        var mp = new MapPoint(lon, lat);
                        g.Geometry = _mercator.FromGeographic(mp);

                        //g.Symbol = new PictureMarkerSymbol() { Source = ims, Width = iconsize, Height = iconsize, OffsetX = iconsize / 2, OffsetY = iconsize/2 };
                        var pd = new ResourceDictionary { Source = new Uri("/csCommon;component/Resources/Styles/PresenterDictionary.xaml", UriKind.Relative) };
                        g.Symbol = pd["FeatureSymbol"] as MarkerSymbol;

                        //g.MouseLeftButtonDown += (e, s) =>
                        //{
                        //    //Explorer.SelectFolder(path);
                        //    //s.Handled = true;
                        //};

                        Graphics.Graphics.Add(g);
                    }
                    catch {}
                }
            }
        }

        void i_PreviewTouchDown(object sender, System.Windows.Input.TouchEventArgs e)
        {
            e.Handled = true;
        }

        void i_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            e.Handled = true;
        }

        void i_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var m = AppStateSettings.Instance.ViewDef.MapControl;
            var gridClick = e.GetPosition(null);
            var screenPointRelativeToHost = m.TransformToVisual(Application.Current.MainWindow).Transform(gridClick);
            
            var items = Graphics.FindGraphicsInHostCoordinates(screenPointRelativeToHost).ToList();
            if (items.Count > 0)
            {
                var g = items.First();
                var p = g.Attributes["path"].ToString();
                if (!string.IsNullOrEmpty(p)) Explorer.SelectFolder(p);
            }
            e.Handled = true;   
        }
    }
}