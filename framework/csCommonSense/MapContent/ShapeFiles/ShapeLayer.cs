using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.Windows.Media;
using csShared.Utils;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.FeatureService.Symbols;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Toolkit.DataSources;
using TestShapeFile;
using Shapefile;
using ShapeType = Shapefile.ShapeType;
using Caliburn.Micro;
using SimpleFillSymbol = ESRI.ArcGIS.Client.Symbols.SimpleFillSymbol;
using csShared;
using System.Xml.Serialization;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;

namespace csGeoLayers.ShapeFiles
{
    [Serializable]
    public class ShapeLayer
    {
        #region Delegates

        /// <summary>
        /// Defines the prototype for a method that reads a block
        /// of shapefile records. Such a method is intended to be
        /// executed by the Dispatcher.
        /// </summary>
        /// <param name="info">Shapefile read information.</param>
        public delegate void ReadNextPrototype(ShapeFileReadInfo info);

        /// <summary>
        /// Defines the prototype for a method that creates and displays
        /// a set of WPF shapes. Such a method is intended to be
        /// executed by the Dispatcher.
        /// </summary>
        /// <param name="info">Shapefile read information.</param>
        public delegate void DisplayNextPrototype(ShapeFileReadInfo info);

        #endregion Delegates

        private static AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        // Used during reading of a shapefile.
        private string _folder;
        private BackgroundWorker _bgWorker;

        private readonly SpatialReference spRef = new SpatialReference(4326);

        // Used during creation of WPF shapes.
        private readonly List<System.Windows.Shapes.Shape> shapeList = new List<System.Windows.Shapes.Shape>();

        public event PropertyChangedEventHandler PropertyChanged;

        //private GraphicsLayer _layer;
        private GroupLayer _gl;

        private void NotifyPropertyChanged(String info)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(info));
        }

        public void Init()
        {
            var folder = AppState.Config.Get("ShapeLayer.Folder", "");
            if (String.IsNullOrEmpty(folder)) return;
            _folder = folder;
            var groupName = AppState.Config.Get("ShapeLayer.GroupName", "Shapes");
            var layer = new GroupLayer {ID = groupName};

            layer.Initialize();
            layer.Visible = true;
            _gl = layer;

            _bgWorker = new BackgroundWorker();
            _bgWorker.DoWork += _bgWorker_DoWork;
            _bgWorker.RunWorkerCompleted += _bgWorker_RunWorkerCompleted;
            _bgWorker.RunWorkerAsync();
        }

        private void _bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Add layer
            Execute.OnUIThread(() => AppState.ViewDef.Layers.ChildLayers.Add(_gl));
        }

        private void _bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            ReadFolder(_folder, _gl);
        }

        /// <summary>
        /// Reads the folder that is configured in the settings and creates layers for the files in the folders.
        /// </summary>
        public void ReadFolder(string path, GroupLayer curGrLayer)
        {
            //Canvas cnvs = new Canvas();
            //dispatcher = cnvs.Dispatcher;
            //string path = @"D:\Projects\TNO\bitBucket\apps\ufData\Forest2000";
            Execute.OnUIThread(() =>
                                   {
                                       foreach (var file in Directory.EnumerateFiles(path))
                                       {
                                           if (file.ToLower().EndsWith(".shp"))
                                           {
                                               AddShapefile(file, curGrLayer); // Hier wordt de locale shape geladen. 
                                           }
                                           if(file.ToLower().EndsWith(".kml"))
                                           {
                                               AddKmlFile(file, curGrLayer);
                                           }
                                       }
                                       foreach (var folder in Directory.EnumerateDirectories(path))
                                       {
                                           // Add Subfolders as new grouplayers
                                           var di = new DirectoryInfo(folder);
                                           var newGl = new GroupLayer {ID = di.Name};
                                           newGl.Initialize();
                                           curGrLayer.ChildLayers.Add(newGl);
                                           ReadFolder(folder, newGl);
                                       }
                                   });
        }

        public static void AddKmlFile(string filePath, GroupLayer curGrLayer)
        {
            var fi = new FileInfo(filePath);
            var kmlLayer = new KmlLayer { Url = new Uri(fi.FullName), ID = fi.Name} ;
            kmlLayer.Initialize();
            kmlLayer.Visible = false;
            curGrLayer.ChildLayers.Add(kmlLayer);
        }

        public void AddShapefile(string filePath, GroupLayer curGrLayer)
        {
            Execute.OnUIThread(() =>
            {
                var grLay = new ShapeGraphicsLayer(filePath);
                //var grLay = new GraphicsLayer();
                var fi = new FileInfo(filePath);
                var settingsXml = fi.Directory + "/" + fi.Name + ".xml";
                var settings = new ShapeLayerSettings();
                if (File.Exists(settingsXml))
                {
                    settings = GetSettings(settingsXml);
                }
                grLay.ID = fi.Name;
                grLay.Visible = false;
                grLay.Initialize();
                curGrLayer.ChildLayers.Add(grLay);
                using (var shapeFile = new Shapefile.Shapefile(filePath))
                {
                    foreach (var shape in shapeFile)
                    {
                                               //string[] metadataNames = shape.GetMetadataNames();
                                               // if (metadataNames != null)
                                               // {
                                               //     Console.WriteLine("Metadata:");
                                               //     var str = string.Empty;
                                               //     foreach (string metadataName in metadataNames)
                                               //     {
                                               //         str += String.Format("{0}={1} ({2})", metadataName, shape.GetMetadata(metadataName), shape.DataRecord.GetDataTypeName(shape.DataRecord.GetOrdinal(metadataName))) + Environment.NewLine;
                                               //     }
                                               //     Console.WriteLine();
                                               // }
                        switch (shape.Type)
                        {
                            case ShapeType.Polygon:
                                var shapePolygon = shape as ShapePolygon;
                                if (shapePolygon != null)
                                    foreach (var part in shapePolygon.Parts)
                                    {
                                        //Console.WriteLine("Polygon part:");
                                        var wpfShape = new System.Windows.Shapes.Polygon();
                                        var points = new System.Windows.Media.PointCollection();
                                        foreach (var point in part)
                                        {
                                            //Console.WriteLine("{0}, {1}", point.X, point.Y);
                                            double lat, lon;
                                                           //lat = point.Y;
                                                           //lon = point.X;
                                            CoordinateUtils.Rd2LonLat(point.X, point.Y, out lon, out lat);
                                            wpfShape.Points.Add(new Point(lon, lat));
                                            points.Add(new Point(lon, lat));
                                        }
                                        shapeList.Add(wpfShape);
                                        DrawPolygon(points, grLay, settings);
                                    }
                                break;
                            case ShapeType.PolyLine:
                                var shapePolyline = shape as ShapePolyLine;
                                if (shapePolyline != null)
                                    foreach (var part in shapePolyline.Parts)
                                    {
                                        //Console.WriteLine("Polygon part:");
                                        var wpfShape = new System.Windows.Shapes.Polygon();
                                        var points = new System.Windows.Media.PointCollection();
                                        foreach (var point in part)
                                        {
                                            //Console.WriteLine("{0}, {1}", point.X, point.Y);
                                            //CoordinateUtils.Rd2LonLat(point.X,point.Y,out lon, out lat);
                                            var lat = point.Y;
                                            var lon = point.X;
                                            wpfShape.Points.Add(new Point(lon, lat));
                                            points.Add(new Point(lon, lat));
                                        }
                                        shapeList.Add(wpfShape);
                                        DrawPolyline(points, grLay, settings);
                                    }
                                break;
                        }
                    }
                }
            });
        }

        private void DrawPolygon(IEnumerable<Point> points, GraphicsLayer gl, ShapeLayerSettings settings)
        {
            // Transform geometry in Polyline
            //Dispatcher.Invoke(new System.Action(delegate
            Execute.OnUIThread(() =>
            {
                var graphicsLayer = gl; // emMain.Layers["MyGraphicsLayer"] as GraphicsLayer;
                var poly = new Polygon {SpatialReference = spRef};
                var pointsE = new ESRI.ArcGIS.Client.Geometry.PointCollection();

                foreach (var point in points)
                {
                    pointsE.Add(new MapPoint(point.X, point.Y));
                }

                poly.Rings.Add(pointsE);
                //poly.SpatialReference = _spRef;

                var grpPoly = new Graphic
                {
                    Symbol = new SimpleFillSymbol
                    {
                        BorderBrush = Brushes.Black,
                        BorderThickness = 2,
                        Fill = new SolidColorBrush(settings.LineColor)
                    },
                    Geometry = poly
                };

                if (graphicsLayer != null) graphicsLayer.Graphics.Add(grpPoly);
            });
        }

        private void DrawPolyline(IEnumerable<Point> points, GraphicsLayer gl, ShapeLayerSettings settings)
        {
            // Transform geometry in Polyline
            //Dispatcher.Invoke(new System.Action(delegate
            Execute.OnUIThread(() =>
            {
                var graphicsLayer = gl; // emMain.Layers["MyGraphicsLayer"] as GraphicsLayer;
                var poly = new Polyline {SpatialReference = spRef};
                var pointsE = new ESRI.ArcGIS.Client.Geometry.PointCollection();

                foreach (var point in points)
                {
                    pointsE.Add(new MapPoint(point.X, point.Y));
                }

                poly.Paths.Add(pointsE);
                //poly.SpatialReference = _spRef;

                var grpPoly = new Graphic
                {
                    Symbol = new SimpleLineSymbol
                    {Color = new SolidColorBrush(settings.LineColor), Width = 2},
                    Geometry = poly
                };

                if (graphicsLayer != null) graphicsLayer.Graphics.Add(grpPoly);
            });
        }

        public static ShapeLayerSettings GetSettings(string fileName)
        {
            var settings = new ShapeLayerSettings();
            try
            {
                var xmlSer = new XmlSerializer(settings.GetType());
                using (var sr = new StreamReader(fileName))
                {
                    settings = (ShapeLayerSettings)xmlSer.Deserialize(sr);
                }
            }
            catch (Exception)
            {
               
            }
            return settings;
        }

        public static void WriteSettings(ShapeLayerSettings settings, string fileName)
        {
            var xmlSer = new XmlSerializer(settings.GetType());
            using (var sw = new StreamWriter(fileName))
            {
                xmlSer.Serialize(sw, settings);
            }
        }
    }

    [Serializable]
    public class ShapeLayerSettings
    {
        private Color lineColor = Colors.Black;

        //[XmlIgnore]
        public Color LineColor
        {
            get { return lineColor; }
            set { lineColor = value; }
        }

        /*[XmlElement("LineColor")]
        public string LineColorHtml
        {
            get { return ColorTranslator.ToHtml(lineColor); lineColor.T }
            set { lineColor = ColorTranslator.FromHtml(value); }
        }*/
    }
}