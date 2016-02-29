using System.Collections.ObjectModel;
using System.Runtime.Serialization.Formatters.Binary;
using Caliburn.Micro;
using csDataServerPlugin;
using csModels.FieldOfViewModel;
using csShared;
using csShared.Utils;
using DataServer;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using ESRI.ArcGIS.Client.Symbols;
using Newtonsoft.Json;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using TestFoV.FieldOfViewService;
using Color = System.Drawing.Color;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using VerticalAlignment = System.Windows.VerticalAlignment;

namespace FieldOfViewModel
{
    public class FoVPoi : ModelPoiBase
    {
        private static readonly AppStateSettings AppState = AppStateSettings.Instance;

        private static readonly BasicHttpBinding DefaultBinding = new BasicHttpBinding
        {
            MaxBufferPoolSize = 67108864,
            MaxBufferSize = 67108864,
            MaxReceivedMessageSize = 67108864,
            TransferMode = TransferMode.Streamed,
            ReaderQuotas = new XmlDictionaryReaderQuotas
            {
                MaxDepth = 32,
                MaxStringContentLength = 5242880,
                MaxArrayLength = 2147483646,
                MaxBytesPerRead = 4096,
                MaxNameTableCharCount = 5242880
            }
        };

        private readonly FieldOfViewServiceClient remoteClient = new FieldOfViewServiceClient(DefaultBinding, new EndpointAddress(new Uri(AppState.Config.Get("FieldOfView.OnlineEndPointUrl", "http://cool3.sensorlab.tno.nl:8035/FieldOfView"))));
        private readonly FieldOfViewServiceClient localClient = new FieldOfViewServiceClient(DefaultBinding, new EndpointAddress(new Uri(AppState.Config.Get("FieldOfView.OfflineEndPointUrl", "http://localhost:8035/FieldOfView"))));

        //private readonly FieldOfViewServiceClient client = new FieldOfViewServiceClient();
        private readonly WebMercator webMercator = new WebMercator();

        private Image image = new Image();

        public ElementLayer ImageLayer { private get; set; }

        public GraphicsLayer GraphicsLayer { private get; set; }

        private System.Windows.Media.Color color;
        private int strokeWidth = 2;
        private int precision = 50;

        //private FoVModel FieldOfViewModel { get { return Model as FoVModel; } }

        private FieldOfViewOperatingMode operatingMode = FieldOfViewOperatingMode.Unknown;

        private FieldOfViewOperatingMode OperatingMode
        {
            get
            {
                return FieldOfViewOperatingMode.Polygon;
                if (operatingMode != FieldOfViewOperatingMode.Unknown) return operatingMode;
                operatingMode = FieldOfViewOperatingMode.Image;
                var fovModeParameter = Model.Model.Parameters.FirstOrDefault(p => string.Equals(p.Name, "OperatingMode"));
                if (fovModeParameter != null) Enum.TryParse(fovModeParameter.Value, true, out operatingMode);
                return operatingMode;
            }
        }

        public override void Start()
        {
            base.Start();

            var baseLayer = ((dsBaseLayer)Model.Layer);
            if (OperatingMode == FieldOfViewOperatingMode.Image)
            {
                if (ImageLayer != null) return;
                var displayName = Model.Id + " FoV img";
                ImageLayer = baseLayer.ChildLayers.OfType<ElementLayer>().FirstOrDefault(c => string.Equals(c.DisplayName, displayName, StringComparison.InvariantCultureIgnoreCase));
                if (ImageLayer == null)
                {
                    ImageLayer = new ElementLayer { ID = displayName, DisplayName = displayName };
                    ImageLayer.Initialize();
                    baseLayer.ChildLayers.Insert(0, ImageLayer);
                    AppState.ViewDef.UpdateLayers();
                }
            }
            else
            {
                if (GraphicsLayer != null) return;
                var displayName = Model.Id + " FoV";
                GraphicsLayer = baseLayer.ChildLayers.OfType<GraphicsLayer>().FirstOrDefault(c => string.Equals(c.DisplayName, displayName, StringComparison.InvariantCultureIgnoreCase));
                if (GraphicsLayer == null)
                {
                    GraphicsLayer = new GraphicsLayer { ID = displayName, DisplayName = displayName };
                    GraphicsLayer.Initialize();
                    baseLayer.ChildLayers.Insert(0, GraphicsLayer);
                    AppState.ViewDef.UpdateLayers();
                }
            }

            color = Model.Model.GetColor("Color", Colors.Blue);
            strokeWidth = Model.Model.GetInt("StrokeWidth", 2);
            precision = Model.Model.GetInt("Precision", 2);

            ManagePoiVisibility();
            //this.operatingMode = FieldOfViewOperatingMode.Polygon;

            switch (OperatingMode)
            {
                case FieldOfViewOperatingMode.Polygon:
                    if (GraphicsLayer == null) return;
                    remoteClient.ComputeFieldOfViewAsVectorCompleted += ClientOnComputeFieldOfViewAsVectorCompleted;
                    localClient.ComputeFieldOfViewAsVectorCompleted += ClientOnComputeFieldOfViewAsVectorCompleted;
                    break;
                default:
                    if (ImageLayer == null) return;
                    remoteClient.ComputeFieldOfViewAsImageCompleted += ClientOnComputeFieldOfViewAsImageCompleted;
                    localClient.ComputeFieldOfViewAsImageCompleted += ClientOnComputeFieldOfViewAsImageCompleted;

                    image = new Image
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        Stretch = Stretch.Fill,
                        StretchDirection = StretchDirection.Both
                    };

                    ElementLayer.SetEnvelope(image, AppStateSettings.Instance.ViewDef.MapControl.Extent);
                    ImageLayer.Children.Add(image);
                    break;
            }

            var posChanged = Observable.FromEventPattern<PositionEventArgs>(ev => Poi.PositionChanged += ev, ev => Poi.PositionChanged -= ev);
            posChanged.Throttle(TimeSpan.FromMilliseconds(150)).Subscribe(k => Calculate());

            var labelChanged = Observable.FromEventPattern<LabelChangedEventArgs>(ev => Poi.LabelChanged += ev, ev => Poi.LabelChanged -= ev);
            labelChanged.Throttle(TimeSpan.FromMilliseconds(150)).Subscribe(k => Calculate());
            Calculate();
        }

        private void ManagePoiVisibility()
        {
            if (!Poi.Data.ContainsKey("layer") || !(Poi.Data["layer"] is Layer)) return;
            var layer = (Layer)Poi.Data["layer"];
            layer.PropertyChanged += (f, b) =>
            {
                if (b.PropertyName == "Visible") image.Visibility = (layer.Visible) ? Visibility.Visible : Visibility.Collapsed;
            };
        }

        public List<Point> Points = new List<Point>();

        public override void Stop()
        {
            base.Stop();
            switch (OperatingMode)
            {
                case FieldOfViewOperatingMode.Polygon:
                    remoteClient.ComputeFieldOfViewAsVectorCompleted -= ClientOnComputeFieldOfViewAsVectorCompleted;
                    localClient.ComputeFieldOfViewAsVectorCompleted -= ClientOnComputeFieldOfViewAsVectorCompleted;
                    GraphicsLayer.ClearGraphics();
                    break;
                default:
                    remoteClient.ComputeFieldOfViewAsImageCompleted -= ClientOnComputeFieldOfViewAsImageCompleted;
                    localClient.ComputeFieldOfViewAsImageCompleted -= ClientOnComputeFieldOfViewAsImageCompleted;
                    if (ImageLayer.Children.Contains(image)) ImageLayer.Children.Remove(image);
                    break;
            }
        }

        public void SetLabel(string label, string value)
        {
            var l = Model.Id + "." + label;
            Poi.Labels[l] = value;

        }

        public void Calculate() {
            var startAngleLabel = Model.Id + ".StartAngle";
            if (!Poi.Labels.ContainsKey(startAngleLabel)) Poi.Labels[startAngleLabel] = "0";
            var startAngle = double.Parse(Poi.Labels[startAngleLabel], CultureInfo.InvariantCulture);

            var distanceLabel = Model.Id + ".Distance";
            if (!Poi.Labels.ContainsKey(distanceLabel)) Poi.Labels[distanceLabel] = "1000";
            var distance = double.Parse(Poi.Labels[distanceLabel], CultureInfo.InvariantCulture);

            var viewAngleLabel = Model.Id + ".ViewAngle";
            if (!Poi.Labels.ContainsKey(viewAngleLabel)) Poi.Labels[viewAngleLabel] = "45";
            var viewAngle = double.Parse(Poi.Labels[viewAngleLabel], CultureInfo.InvariantCulture);

            var heightLabel = Model.Id + ".Height";
            if (!Poi.Labels.ContainsKey(heightLabel)) Poi.Labels[heightLabel] = "2";
            var height = double.Parse(Poi.Labels[heightLabel], CultureInfo.InvariantCulture);

            var enabledLabel = Model.Id + ".Enabled";
            if (!Poi.Labels.ContainsKey(enabledLabel)) Poi.Labels[enabledLabel] = "true";
            var isEnabled = bool.Parse(Poi.Labels[enabledLabel]);

            var domainLabel = Model.Id + ".Domain";
            if (!Poi.Labels.ContainsKey(domainLabel)) Poi.Labels[domainLabel] = "Air";
            var domain = Poi.Labels[domainLabel];

            switch (OperatingMode) {
                case FieldOfViewOperatingMode.Polygon:
                    if (distance.IsZero() || viewAngle.IsZero() || !isEnabled) {
                        Execute.OnUIThread(ClearGraphics);
                    }

                    var vectorRequest      = new FieldOfViewAsVectorRequest {
                        CameraLocation     = new Location {
                            Latitude       = Poi.Position.Latitude,
                            Longitude      = Poi.Position.Longitude,
                            Altitude       = height
                        },
                        MinVisualRange     = 0,
                        MaxVisualRange     = distance,
                        DesiredFieldOfView = distance,
                        ViewAngle          = (int) viewAngle,
                        Mode               = ModeOfOperation.NormalRadar,
                        Precision          = precision,
                        Orientation        = (int) startAngle + (int) Poi.Orientation
                    };

                    switch (domain.ToLower()) {
                        case "surface":
                            vectorRequest.Mask = Masks.Water;
                            break;
                        case "ground":
                            vectorRequest.Mask = Masks.Land;
                            break;
                        default:
                            vectorRequest.Mask = Masks.None;
                            break;
                    }

                    var json = JsonConvert.SerializeObject(vectorRequest);
                    var hash = json.GetHashCode();
                    var folder = Path.Combine(AppStateSettings.CacheFolder, "fov");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                    var file = Path.Combine(folder, hash + ".res");
                    remoteClient.InnerChannel.OperationTimeout = new TimeSpan(0, 0, 0, 10);
                    var loaded = false;
                    if (File.Exists(file)) {
                        try {
                            var stream = File.Open(file, FileMode.Open);
                            var bformatter = new BinaryFormatter();
                            var e = (FieldOfViewAsVectorResponse) bformatter.Deserialize(stream);
                            stream.Close();
                            ClientOnComputeFieldOfViewAsVectorCompleted(this, new ComputeFieldOfViewAsVectorCompletedEventArgs(new object[] {e}, null, false, null));
                            loaded = true;
                        }
                        catch (Exception) {}
                    }
                    if (!loaded) {
                        if (AppState.IsOnline)
                            remoteClient.ComputeFieldOfViewAsVectorAsync(vectorRequest, file);
                        else
                            localClient.ComputeFieldOfViewAsVectorAsync(vectorRequest);
                    }
                    break;
                default:
                    Execute.OnUIThread(() => {
                        image.Visibility = (Poi.IsVisible) ? Visibility.Visible : Visibility.Collapsed;
                        if (Math.Abs(distance) < 0.00001 || Math.Abs(viewAngle) < 0.00001 || !isEnabled)
                            image.Source = null;
                    });

                    var request2 = new FieldOfViewAsImageRequest {
                        CameraLocation = new Location {
                            Latitude = Poi.Position.Latitude,
                            Longitude = Poi.Position.Longitude,
                            Altitude = height
                        },
                        MinVisualRange = 0,
                        MaxVisualRange = distance,
                        DesiredFieldOfView = distance,
                        ViewAngle = (int) viewAngle,
                        Mode = ModeOfOperation.NormalRadar,
                        Color = Color.FromArgb(color.A, color.R, color.G, color.B),
                        Orientation = (int) startAngle + (int) Poi.Orientation
                    };
                    switch (domain.ToLower()) {
                        case "surface":
                            request2.Mask = Masks.Water;
                            break;
                        case "ground":
                            request2.Mask = Masks.Land;
                            break;
                        default:
                            request2.Mask = Masks.None;
                            break;
                    }

                    if (AppState.IsOnline)
                        remoteClient.ComputeFieldOfViewAsImageAsync(request2);
                    else
                        localClient.ComputeFieldOfViewAsImageAsync(request2);
                    break;
            }
        }

        private void ClientOnComputeFieldOfViewAsVectorCompleted(object sender, ComputeFieldOfViewAsVectorCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Logger.Log("FieldOfViewModel.FovPoi", "Error generating FoV", e.Error.ToString(), Logger.Level.Error, true);
                return;
            }

            if ((e.Result == null || e.Result.Locations == null) || (e.Result.Locations.Count == 0)) return; // No FoV

            if (e.UserState != null)
            {
                Stream stream = File.Open(e.UserState.ToString(), FileMode.Create);
                BinaryFormatter bformatter = new BinaryFormatter();
                bformatter.Serialize(stream, e.Result);
                stream.Close();
            }

            //Kml.CreateKmlFile(@"c:\temp\fov.kml", e.Result.Locations.ToList());

            // Create polygon for map
            var coordinates = new ESRI.ArcGIS.Client.Geometry.PointCollection();
            var pts = new List<Point>();
            var camera = (MapPoint)webMercator.FromGeographic(new MapPoint(e.Result.RequestMessage.CameraLocation.Longitude, e.Result.RequestMessage.CameraLocation.Latitude));
            if (e.Result.RequestMessage.ViewAngle != 360) coordinates.Add(camera);
            foreach (var pnt in e.Result.Locations.Select(x => new MapPoint(x.Longitude, x.Latitude)))
            {
                pts.Add(new Point(pnt.X, pnt.Y));
                coordinates.Add((MapPoint)webMercator.FromGeographic(pnt)); /* Convert from WGS84 to Mercator projection */
            }
            Points = pts;
            if (e.Result.RequestMessage.ViewAngle != 360) coordinates.Add(camera);

            // Fill POI points
            //Poi.Points.Clear();
            //foreach (var pnt in e.Result.Locations.Select(x => new Point(x.Longitude, x.Latitude)))
            //{
            //    Poi.Points.Add(pnt);
            //}

            Execute.OnUIThread(() =>
            {
                try
                {
                    var pl = new Polyline();
                    pl.Paths.Add(coordinates);
                    var g = new Graphic
                    {
                        Symbol = new SimpleLineSymbol
                        {
                            Color = new SolidColorBrush(color),
                            Width = strokeWidth,
                            Style = SimpleLineSymbol.LineStyle.Solid
                        },
                        Geometry = pl
                    };
                    g.Attributes["ID"] = Poi.Id.ToString();

                    ClearGraphics();
                    GraphicsLayer.Graphics.Add(g);
                }
                catch (Exception ex)
                {
                    Logger.Log("FieldOfViewModel.FovPoi", "Error generating FoV", ex.Message, Logger.Level.Error, true);
                }
            });
        }

        private void ClearGraphics()
        {
            var id = Poi.Id.ToString();
            var existingGraphic = GraphicsLayer.Graphics.FirstOrDefault(g => string.Equals(id, g.Attributes["ID"].ToString(), StringComparison.InvariantCultureIgnoreCase));
            if (existingGraphic != null) GraphicsLayer.Graphics.Remove(existingGraphic);
        }

        ///// <summary>
        ///// Convert to Radians.
        ///// </summary>      
        //private static double ToRadian(double val)
        //{
        //    return (Math.PI / 180) * val;
        //}

        //private static double ToDegrees(double val)
        //{
        //    return (180 / Math.PI) * val;
        //}

        //private const double EarthRadius = 6378137.0;

        ///// <summary>
        ///// Calculates the end-point from a given source at a given range (meters) and bearing (degrees).
        ///// This methods uses simple geometry equations to calculate the end-point.
        ///// </summary>
        ///// <param name="source">Point of origin</param>
        ///// <param name="range">Range in meters</param>
        ///// <param name="bearing">Bearing in degrees</param>
        ///// <returns>End-point from the source given the desired range and bearing.</returns>
        //public static Point CalculateDerivedPosition(Point source, double range, double bearing)
        //{
        //    var latA = ToRadian(source.Y);
        //    var lonA = ToRadian(source.X);
        //    var angularDistance = range / EarthRadius;
        //    var trueCourse = ToRadian(bearing);

        //    var lat = Math.Asin(Math.Sin(latA) * Math.Cos(angularDistance) + Math.Cos(latA) * Math.Sin(angularDistance) * Math.Cos(trueCourse));
        //    var dlon = Math.Atan2(Math.Sin(trueCourse) * Math.Sin(angularDistance) * Math.Cos(latA), Math.Cos(angularDistance) - Math.Sin(latA) * Math.Sin(lat));
        //    var lon = ((lonA + dlon + Math.PI) % (Math.PI * 2)) - Math.PI;

        //    return new Point(ToDegrees(lon), ToDegrees(lat));
        //}

        private void ClientOnComputeFieldOfViewAsImageCompleted(object sender, ComputeFieldOfViewAsImageCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                AppStateSettings.Instance.TriggerNotification("Error generating FoV", "FoV Error", Brushes.Red);
                return;
            }
            var result = e.Result;
            if (result == null || result.ByteBuffer == null) return;

            var bitmapImage = ConvertToBitmapImage(result);
            var env = webMercator.FromGeographic(new Envelope(e.Result.BoundingBox.MinimumLongitude, e.Result.BoundingBox.MinimumLatitude, e.Result.BoundingBox.MaximumLongitude, e.Result.BoundingBox.MaximumLatitude)) as Envelope;
            Execute.OnUIThread(() =>
            {
                image.Source = bitmapImage;
                ElementLayer.SetEnvelope(image, env);
            });
        }

        private static BitmapImage ConvertToBitmapImage(FieldOfViewAsImageResponse result)
        {
            var bitmapImage = new BitmapImage();
            using (var mem = new MemoryStream(result.ByteBuffer))
            {
                mem.Position = 0;
                bitmapImage.BeginInit();
                bitmapImage.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.UriSource = null;
                bitmapImage.StreamSource = mem;
                bitmapImage.EndInit();
            }
            bitmapImage.Freeze();
            return bitmapImage;
        }
    }
}