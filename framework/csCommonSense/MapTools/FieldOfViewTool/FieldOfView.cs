
using System.ServiceModel;
using System.Xml;
using Caliburn.Micro;
using csShared;
using csShared.Geo;
using csShared.Utils;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using ESRI.ArcGIS.Client.Symbols;
using IMB3;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TestFoV.FieldOfViewService;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Drawing.Color;
using Point = System.Windows.Point;
using PointCollection = ESRI.ArcGIS.Client.Geometry.PointCollection;

namespace csGeoLayers.MapTools.FieldOfViewTool
{
    public class FieldOfView : PropertyChangedBase
    {
        private readonly Timer updateTimer = new Timer();
        private readonly WebMercator webMercator = new WebMercator();

        public csPoint FinishPoint;
        public GroupLayer Layer;
        public Graphic Line;

        public ElementLayer ImageLayer;
        public Image Image;

        public GraphicsLayer MLayer;
        public csPoint StartPoint;

        private TEventEntry _3D;
        private Graphic attachedFinish;
        private Graphic attachedStart;

        private double distance;
        public Graphic Finish;
        private bool firstMovement = true;

        private string lastMes;
        private bool rotating;
        private string rotatingState;
        public Graphic Start;

        /*
        <binding name                                           ="BasicHttpBinding_IFieldOfViewService" maxBufferPoolSize="67108864" maxBufferSize="67108864" maxReceivedMessageSize="67108864" transferMode="Streamed">
          <readerQuotas maxDepth                                ="32" maxStringContentLength="5242880" maxArrayLength="2147483646" maxBytesPerRead="4096" maxNameTableCharCount="5242880" />
        </binding>
         */
        private static readonly BasicHttpBinding DefaultBinding = new BasicHttpBinding {
            MaxBufferPoolSize          = 67108864, 
            MaxBufferSize              = 67108864, 
            MaxReceivedMessageSize     = 67108864,
            TransferMode               = TransferMode.Streamed,
            ReaderQuotas               = new XmlDictionaryReaderQuotas {
                MaxDepth               = 32, 
                MaxStringContentLength = 5242880, 
                MaxArrayLength         = 2147483646, 
                MaxBytesPerRead        = 4096, 
                MaxNameTableCharCount  = 5242880
            }
        };
        //<!--<endpoint address="http://cool3.sensorlab.tno.nl:8035/FieldOfView" binding="basicHttpBinding" bindingConfiguration="BasicHttpBinding_IFieldOfViewService" contract="FieldOfViewService.IFieldOfViewService" name="OnlineFieldOfViewService" />
        //<endpoint address="http://localhost:8035/FieldOfView" binding="basicHttpBinding" bindingConfiguration="BasicHttpBinding_IFieldOfViewService" contract="FieldOfViewService.IFieldOfViewService" name="LocalFieldOfViewService" />-->
        private readonly FieldOfViewServiceClient remoteClient = new FieldOfViewServiceClient(DefaultBinding, new EndpointAddress(new Uri(AppState.Config.Get("FieldOfView.OnlineEndPointUrl", "http://cool3.sensorlab.tno.nl:8035/FieldOfView"))));
        private readonly FieldOfViewServiceClient localClient = new FieldOfViewServiceClient(DefaultBinding, new EndpointAddress(new Uri(AppState.Config.Get("FieldOfView.OfflineEndPointUrl", "http://localhost:8035/FieldOfView"))));

        private bool autoHeight = true;
        private DispatcherTimer dtr;

        private static AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public double Distance
        {
            get { return distance; }
            set
            {
                distance = value;
                NotifyOfPropertyChange(() => Distance);
            }
        }

        public bool AutoHeight
        {
            get { return autoHeight; }
            set
            {
                autoHeight = value;
                NotifyOfPropertyChange(() => AutoHeight);
            }
        }

        public double GetDistance()
        {
            var p1 = webMercator.ToGeographic(StartPoint.Mp) as MapPoint;
            var p2 = webMercator.ToGeographic(FinishPoint.Mp) as MapPoint;
            if (p1 == null || p2 == null) return 0;
            var pLon1 = p1.X;
            var pLat1 = p1.Y;
            var pLon2 = p2.X;
            var pLat2 = p2.Y;

            var dist = CoordinateUtils.Distance(pLat1, pLon1, pLat2, pLon2, 'K');
            return dist; //Math.Sqrt((deltaX*deltaX) + (deltaY*deltaY));
        }


        public void Init(GroupLayer gl, MapPoint start, MapPoint finish, ResourceDictionary rd)
        {
            remoteClient.ComputeFieldOfViewAsImageCompleted += ClientOnComputeFieldOfViewCompleted;
            localClient .ComputeFieldOfViewAsImageCompleted += ClientOnComputeFieldOfViewCompleted;

            StartPoint = new csPoint
            {
                Mp = start
            };
            FinishPoint = new csPoint
            {
                Mp = finish
            };
            MLayer = new GraphicsLayer
            {
                ID = Guid.NewGuid().ToString()
            };

            ImageLayer = new ElementLayer();
            Image = new System.Windows.Controls.Image
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Stretch = Stretch.Fill,
                StretchDirection = StretchDirection.Both
            };
            ElementLayer.SetEnvelope(Image, AppState.ViewDef.MapControl.Extent);
            ImageLayer.Children.Add(Image);

            Start = new Graphic();
            Finish = new Graphic();
            Line = new Graphic();

            var ls = new LineSymbol
            {
                Color = Brushes.Black,
                Width = 4
            };
            Line.Symbol = ls;
            UpdateLine();

            MLayer.Graphics.Add(Line);

            Start.Geometry = start;
            Start.Symbol = rd["Start"] as Symbol;
            Start.Attributes["position"] = start;
            Start.Attributes["finish"] = Finish;
            Start.Attributes["start"] = Start;
            Start.Attributes["line"] = Line;
            Start.Attributes["state"] = "start";
            Start.Attributes["measure"] = this;
            Start.Attributes["menuenabled"] = true;
            MLayer.Graphics.Add(Start);

            Finish.Geometry = finish;
            Finish.Attributes["position"] = finish;
            Finish.Symbol = rd["Finish"] as Symbol;
            Finish.Attributes["finish"] = Finish;
            Finish.Attributes["start"] = Start;
            Finish.Attributes["line"] = Line;
            Finish.Attributes["measure"] = this;
            Finish.Attributes["state"] = "finish";
            Finish.Attributes["menuenabled"] = true;
            MLayer.Graphics.Add(Finish);
            Layer.ChildLayers.Add(ImageLayer);
            Layer.ChildLayers.Add(MLayer);
            MLayer.Initialize();

            AppStateSettings.Instance.ViewDef.MapManipulationDelta += ViewDef_MapManipulationDelta;

            if (AppState.Imb != null && AppState.Imb.Imb != null)
            {
                _3D = AppState.Imb.Imb.Publish(AppState.Imb.Imb.ClientHandle + ".3d");
                //AppState.Imb.Imb.Publish(_channel);
            }

            updateTimer.Interval = 50;
            updateTimer.Elapsed += UpdateTimerElapsed;
            updateTimer.Start();
        }

        private void ClientOnComputeFieldOfViewCompleted(object sender, ComputeFieldOfViewAsImageCompletedEventArgs e) {
            if (e.Error != null) {
                Logger.Log("Field of View", "Error showing field of view result", e.Error.Message, Logger.Level.Error, true);
                return;
            }
            var result = e.Result;
            if (result == null || result.ByteBuffer == null) return;

            var image = ConvertToBitmapImage(result);
            Execute.OnUIThread(() => { Image.Source = image; });

            var env = webMercator.FromGeographic(new Envelope(e.Result.BoundingBox.MinimumLongitude, e.Result.BoundingBox.MinimumLatitude, e.Result.BoundingBox.MaximumLongitude, e.Result.BoundingBox.MaximumLatitude)) as Envelope;
            ElementLayer.SetEnvelope(Image, env);
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

        //private static Image ByteArrayToImage(byte[] byteArrayIn)
        //{
        //    using (var ms = new MemoryStream(byteArrayIn))
        //    {
        //        return System.Drawing.Image.FromStream(ms);
        //    }
        //}

        private void UpdateTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (AppState.Imb != null && AppState.Imb.IsConnected)
            {
                if (!string.IsNullOrEmpty(lastMes)) _3D.SignalString(lastMes);
            }
            lastMes = "";
        }

        private void ViewDef_MapManipulationDelta(object sender, EventArgs e)
        {
            UpdateLine();
        }

        public void RemoveImage()
        {
            Image.Source = null;
        }

        public void Calculate()
        {
            if (StartPoint  == null || StartPoint .Mp == null) return;
            if (FinishPoint == null || FinishPoint.Mp == null) return;
            var d = GetDistance() * 1000;
            var mp = webMercator.ToGeographic(StartPoint.Mp) as MapPoint;
            if (mp == null) return;
            var request2 = new FieldOfViewAsImageRequest
            {
                CameraLocation = new Location
                {
                    Longitude      = mp.X,
                    Latitude       = mp.Y,
                    Altitude       = 2
                },
                MinVisualRange     = 0,
                MaxVisualRange     = d,
                DesiredFieldOfView = d,
                Orientation        = 0,
                ViewAngle          = 360,
                Mode               = ModeOfOperation.NormalRadar,
                Mask               = Masks.None,
                Color              = Color.FromArgb(128, Color.Blue)
            };
            if (AppState.IsOnline)
                remoteClient.ComputeFieldOfViewAsImageAsync(request2);
            else
                localClient.ComputeFieldOfViewAsImageAsync(request2);
        }

        private void UpdateLine()
        {
            var pl = new Polyline
            {
                Paths = new ObservableCollection<PointCollection>()
            };
            var pc = new PointCollection { StartPoint.Mp, FinishPoint.Mp };
            pl.Paths.Add(pc);
            Line.Geometry = pl;
        }

        internal void Remove()
        {
            RemoveImage();
            Layer.ChildLayers.Remove(MLayer);
            Layer.ChildLayers.Remove(ImageLayer);
            StopRotation();
        }

        public void Attach(string state, Graphic g)
        {
            if (g == null) return;
            firstMovement = true;
            firstMoveState = state;
            switch (state)
            {
                case "start":
                    attachedStart = g;
                    UpdatePoint("start", attachedStart.Geometry as MapPoint);
                    g.PropertyChanged += (e, s) =>
                                             {
                                                 if (s.PropertyName == "Geometry" && attachedStart != null)
                                                 {
                                                     UpdatePoint("start", attachedStart.Geometry as MapPoint);
                                                 }
                                             };
                    break;
                case "finish":
                    attachedFinish = g;
                    UpdatePoint("finish", attachedFinish.Geometry as MapPoint);
                    g.PropertyChanged += (e, s) =>
                                             {
                                                 if (s.PropertyName == "Geometry" && attachedFinish != null)
                                                 {
                                                     UpdatePoint("finish", attachedFinish.Geometry as MapPoint);
                                                 }
                                             };
                    break;
            }
        }

        private string firstMoveState;
        private double rotateFixAngle;

        public bool RotateFix { get; set; }

        public void FixRotation(string state)
        {
            var sp = webMercator.ToGeographic(StartPoint.Mp) as MapPoint;
            var fp = webMercator.ToGeographic(FinishPoint.Mp) as MapPoint;
            if (fp == null || sp == null) return;
            RotateFix = true;
            var c1 = AppStateSettings.Instance.ViewDef.MapPoint(new KmlPoint(sp.X, sp.Y));
            var c2 = AppStateSettings.Instance.ViewDef.MapPoint(new KmlPoint(fp.X, fp.Y));
            var angle = 360 - Angle(c1.X, c1.Y, c2.X, c2.Y);
            rotateFixAngle = angle % 360;

            switch (state)
            {
                case "start":
                    var o = Convert.ToDouble(attachedStart.Attributes.ContainsKey("Orientation"));
                    attachedStart.AttributeValueChanged += (e, s) => {
                        if (!attachedStart.Attributes.ContainsKey("Orientation")) return;
                        var no = Convert.ToDouble(attachedStart.Attributes["Orientation"]);
                        var na = (no - o) + rotateFixAngle;
                        var d1 = Math.Abs(FinishPoint.Mp.X - StartPoint.Mp.X);
                        d1 = d1*d1;
                        var d2 = Math.Abs(FinishPoint.Mp.Y - StartPoint.Mp.Y);
                        d2 = d2*d2;
                        var d = Math.Sqrt(d1 + d2);

                        FinishPoint.Mp.Y = StartPoint.Mp.Y + (int) Math.Round(d*Math.Cos((na/180)*Math.PI));
                        FinishPoint.Mp.X = StartPoint.Mp.X + (int) Math.Round(d*Math.Sin((na/180)*Math.PI));

                        Console.WriteLine(d);
                    };

                    break;
            }
        }

        internal void UpdatePoint(string state, MapPoint geometry)
        {
            if (firstMovement)
            {
                if (string.IsNullOrEmpty(firstMoveState))
                {
                    firstMoveState = state;
                }
                else if (firstMoveState != state)
                {
                    firstMovement = false;
                }
                else
                {
                    firstMoveState = state;
                }
            }
            switch (state)
            {
                case "start":
                    if (StartPoint == null || Start == null) break;
                    if (firstMovement)
                    {

                        FinishPoint.Mp.X += geometry.X - StartPoint.Mp.X;
                        FinishPoint.Mp.Y += geometry.Y - StartPoint.Mp.Y;
                    }
                    StartPoint.Mp = geometry;
                    Start.Geometry = geometry;
                    break;
                case "finish":
                    if (FinishPoint == null || Finish == null) break;
                    if (firstMovement)
                    {
                        StartPoint.Mp.X += geometry.X - FinishPoint.Mp.X;
                        StartPoint.Mp.Y += geometry.Y - FinishPoint.Mp.Y;
                    }
                    //_firstMovement = false;
                    FinishPoint.Mp = geometry;
                    Finish.Geometry = geometry;
                    break;
            }

            UpdateLine();
            Distance = GetDistance();

        }


        private static double Angle(double px1, double py1, double px2, double py2)
        {
            // Negate X and Y values
            var pxRes = px2 - px1;
            var pyRes = py2 - py1;
            double angle;
            // Calculate the angle
            if (pxRes == 0.0)
            {
                if (pxRes == 0.0)
                    angle = 0.0;
                else if (pyRes > 0.0) angle = Math.PI / 2.0;
                else
                    angle = Math.PI * 3.0 / 2.0;
            }
            else if (pyRes == 0.0)
            {
                angle = pxRes > 0.0
                            ? 0.0
                            : Math.PI;
            }
            else
            {
                if (pxRes < 0.0)
                    angle = Math.Atan(pyRes / pxRes) + Math.PI;
                else if (pyRes < 0.0) angle = Math.Atan(pyRes / pxRes) + (2 * Math.PI);
                else
                    angle = Math.Atan(pyRes / pxRes);
            }
            // Convert to degrees
            angle = angle * 180 / Math.PI;
            return angle;
        }

        private void dtr_Tick(object sender, EventArgs e)
        {
            if (rotating)
            {
                firstMovement = false;

                switch (rotatingState)
                {
                    case "start":
                        var angles = Angle(StartPoint.Mp.X, StartPoint.Mp.Y, FinishPoint.Mp.X, FinishPoint.Mp.Y);
                        var deltaXs = StartPoint.Mp.X - FinishPoint.Mp.X;
                        var deltaYs = StartPoint.Mp.Y - FinishPoint.Mp.Y;

                        var distances = Math.Sqrt((deltaXs * deltaXs) + (deltaYs * deltaYs));
                        angles += 1;
                        angles = ((angles - 180) / 360) * 2 * Math.PI;
                        var ys = (int)Math.Round(FinishPoint.Mp.Y + distances * Math.Sin(angles));
                        ;
                        var xs = (int)Math.Round(FinishPoint.Mp.X + distances * Math.Cos(angles));
                        UpdatePoint("start", new MapPoint(xs, ys));
                        break;
                    case "finish":
                        var angle = Angle(FinishPoint.Mp.X, FinishPoint.Mp.Y, StartPoint.Mp.X, StartPoint.Mp.Y);
                        var deltaX = StartPoint.Mp.X - FinishPoint.Mp.X;
                        var deltaY = StartPoint.Mp.Y - FinishPoint.Mp.Y;

                        var distance = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
                        angle += 1;
                        angle = ((angle - 180) / 360) * 2 * Math.PI;
                        var y = (int)Math.Round(StartPoint.Mp.Y + distance * Math.Sin(angle));
                        ;
                        var x = (int)Math.Round(StartPoint.Mp.X + distance * Math.Cos(angle));
                        UpdatePoint("finish", new MapPoint(x, y));
                        break;
                }
                //MapPoint mp = new MapPoint(this.Position.X + 10, this.Position.Y + 10);
                //UpdatePoint(_state, mp);
            }
            else
            {
                dtr.Stop();
            }
        }

        internal void StopRotation()
        {
            rotating = false;
        }

        internal void StartRotation(string state)
        {
            if (dtr != null && dtr.IsEnabled) dtr.Stop();
            rotatingState = state;
            rotating = true;
            dtr = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 100) };
            dtr.Tick += dtr_Tick;
            dtr.Start();
        }

        internal void Detach(string _state)
        {
            firstMoveState = null;
            switch (_state)
            {
                case "start":
                    attachedStart = null;
                    break;
                case "finish":
                    attachedFinish = null;
                    break;
            }
        }
    }
}