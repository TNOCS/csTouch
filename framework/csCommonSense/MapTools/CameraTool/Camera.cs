using Caliburn.Micro;
using csImb;
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
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using PointCollection = ESRI.ArcGIS.Client.Geometry.PointCollection;

namespace csGeoLayers.MapTools.CameraTool
{
    public class Camera : PropertyChangedBase
    {
        private const double Rad2Deg = 180.0 / Math.PI;
        private readonly Timer _updateTimer = new Timer();
        private readonly WebMercator webMercator = new WebMercator();

        public GroupLayer Layer;
        public Graphic Line;

        public GraphicsLayer MLayer;

        private TEventEntry _3d;
        private Graphic _attachedFinish;
        private Graphic _attachedStart;

        private double _distance;
        public Graphic _finish;
        private string _firstMoveState;
        private bool _firstMovement = true;

        private string _lastMes;
        private double _rotateFixAngle;
        //private double _rotateFixDistance;
        private string _rotateFixState;
        private bool _rotating;
        private string _rotatingState;
        public Graphic _start;


        private bool autoHeight = true;
        private DispatcherTimer dtr;
        public csPoint finishPoint;
        public csPoint startPoint;

        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public double Distance
        {
            get { return _distance; }
            set
            {
                _distance = value;
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

        public bool RotateFix { get; set; }

        public double GetDistance()
        {
            var p1 = webMercator.ToGeographic(startPoint.Mp) as MapPoint;
            var p2 = webMercator.ToGeographic(finishPoint.Mp) as MapPoint;
            if (p1 != null && p2 != null)
            {
                var pLon1 = p1.X;
                var pLat1 = p1.Y;
                var pLon2 = p2.X;
                var pLat2 = p2.Y;

                var dist = CoordinateUtils.Distance(pLat1, pLon1, pLat2, pLon2, 'K');
                return dist; //Math.Sqrt((deltaX*deltaX) + (deltaY*deltaY));
            }
            return 0;
        }


        public void Init(GroupLayer gl, MapPoint start, MapPoint finish, ResourceDictionary rd)
        {
            startPoint = new csPoint
            {
                Mp = start
            };
            finishPoint = new csPoint
            {
                Mp = finish
            };
            MLayer = new GraphicsLayer
            {
                ID = Guid.NewGuid().ToString()
            };
            _start = new Graphic();
            _finish = new Graphic();
            Line = new Graphic();

            var ls = new LineSymbol
            {
                Color = Brushes.Black,
                Width = 4
            };
            Line.Symbol = ls;
            UpdateLine();

            MLayer.Graphics.Add(Line);

            _start.Geometry = start;
            _start.Symbol = rd["Start"] as Symbol;
            _start.Attributes["position"] = start;
            _start.Attributes["finish"] = _finish;
            _start.Attributes["start"] = _start;
            _start.Attributes["line"] = Line;
            _start.Attributes["state"] = "start";
            _start.Attributes["measure"] = this;
            _start.Attributes["menuenabled"] = true;
            MLayer.Graphics.Add(_start);

            _finish.Geometry = finish;
            _finish.Attributes["position"] = finish;
            _finish.Symbol = rd["Finish"] as Symbol;
            _finish.Attributes["finish"] = _finish;
            _finish.Attributes["start"] = _start;
            _finish.Attributes["line"] = Line;
            _finish.Attributes["measure"] = this;
            _finish.Attributes["state"] = "finish";
            _finish.Attributes["menuenabled"] = true;
            MLayer.Graphics.Add(_finish);

            Layer.ChildLayers.Add(MLayer);
            MLayer.Initialize();

            AppStateSettings.Instance.ViewDef.MapManipulationDelta += ViewDef_MapManipulationDelta;

            if (AppState.Imb != null && AppState.Imb.Imb != null)
            {
                _3d = AppState.Imb.Imb.Publish(AppState.Imb.Imb.ClientHandle + ".3d");
                //AppState.Imb.Imb.Publish(_channel);
            }

            _updateTimer.Interval = 50;
            _updateTimer.Elapsed += UpdateTimerElapsed;
            _updateTimer.Start();
        }

        private void UpdateTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (AppState.Imb != null && AppState.Imb.IsConnected)
            {
                if (!string.IsNullOrEmpty(_lastMes)) _3d.SignalString(_lastMes);
            }
            _lastMes = "";
        }

        private void ViewDef_MapManipulationDelta(object sender, EventArgs e)
        {
            UpdateLine();
        }

        internal void UpdateLine()
        {
            var pl = new Polyline
            {
                Paths = new ObservableCollection<PointCollection>()
            };
            var pc = new PointCollection {
                startPoint.Mp,
                finishPoint.Mp
            };

            pl.Paths.Add(pc);
            Line.Geometry = pl;
        }

        internal void Remove()
        {
            Layer.ChildLayers.Remove(MLayer);
            StopRotation();
        }

        public void Attach(string state, Graphic g)
        {
            if (g == null) return;
            _firstMovement = true;
            _firstMoveState = state;
            switch (state)
            {
                case "start":
                    _attachedStart = g;
                    UpdatePoint("start", _attachedStart.Geometry as MapPoint);
                    g.PropertyChanged += (e, s) =>
                    {
                        if (s.PropertyName == "Geometry" && _attachedStart != null)
                        {
                            UpdatePoint("start", _attachedStart.Geometry as MapPoint);
                        }
                    };
                    break;
                case "finish":
                    _attachedFinish = g;
                    UpdatePoint("finish", _attachedFinish.Geometry as MapPoint);
                    g.PropertyChanged += (e, s) =>
                    {
                        if (s.PropertyName == "Geometry" && _attachedFinish != null)
                        {
                            UpdatePoint("finish", _attachedFinish.Geometry as MapPoint);
                        }
                    };
                    break;
            }
        }

        public void FixRotation(string state)
        {
            _rotateFixState = state;
            var sp = webMercator.ToGeographic(startPoint.Mp) as MapPoint;
            var fp = webMercator.ToGeographic(finishPoint.Mp) as MapPoint;
            RotateFix = true;
            var c1 = AppStateSettings.Instance.ViewDef.MapPoint(new KmlPoint(sp.X, sp.Y));
            var c2 = AppStateSettings.Instance.ViewDef.MapPoint(new KmlPoint(fp.X, fp.Y));
            var angle = 360 - Angle(c1.X, c1.Y, c2.X, c2.Y);
            _rotateFixAngle = angle % 360;

            switch (state)
            {
                case "start":
                    var o = Convert.ToDouble(_attachedStart.Attributes.ContainsKey("Orientation"));
                    _attachedStart.AttributeValueChanged += (e, s) =>
                    {
                        if (_attachedStart.Attributes.ContainsKey("Orientation"))
                        {
                            var no = Convert.ToDouble(_attachedStart.Attributes["Orientation"]);
                            var na = (no - o) + _rotateFixAngle;
                            var d1 = Math.Abs(finishPoint.Mp.X - startPoint.Mp.X);
                            d1 = d1 * d1;
                            var d2 = Math.Abs(finishPoint.Mp.Y - startPoint.Mp.Y);
                            d2 = d2 * d2;
                            var d = Math.Sqrt(d1 + d2);

                            finishPoint.Mp.Y = startPoint.Mp.Y + (int)Math.Round(d * Math.Cos((na / 180) * Math.PI));
                            finishPoint.Mp.X = startPoint.Mp.X + (int)Math.Round(d * Math.Sin((na / 180) * Math.PI));

                            Console.WriteLine(d);
                        }
                    };

                    break;
            }
        }


        internal void UpdatePoint(string state, MapPoint geometry)
        {
            if (_firstMovement)
            {
                if (string.IsNullOrEmpty(_firstMoveState))
                {
                    _firstMoveState = state;
                }
                else if (_firstMoveState != state)
                {
                    _firstMovement = false;
                }
                else
                {
                    _firstMoveState = state;
                }
            }
            switch (state)
            {
                case "start":
                    if (startPoint == null || _start == null) break;
                    if (_firstMovement)
                    {
                        finishPoint.Mp.X += geometry.X - startPoint.Mp.X;
                        finishPoint.Mp.Y += geometry.Y - startPoint.Mp.Y;
                    }
                    startPoint.Mp = geometry;
                    _start.Geometry = geometry;
                    break;
                case "finish":
                    if (finishPoint == null || _finish == null) break;
                    if (_firstMovement)
                    {
                        startPoint.Mp.X += geometry.X - finishPoint.Mp.X;
                        startPoint.Mp.Y += geometry.Y - finishPoint.Mp.Y;
                    }
                    //_firstMovement = false;
                    finishPoint.Mp = geometry;
                    _finish.Geometry = geometry;
                    break;
            }

            UpdateLine();
            Distance = GetDistance();
            Send3DMessage();
        }

        public void Send3DMessage()
        {
            var wm = new WebMercator();
            var p = new Pos3D();
            var mpC = (MapPoint)wm.ToGeographic(finishPoint.Mp);
            var mpD = (MapPoint)wm.ToGeographic(startPoint.Mp);
            if (AutoHeight)
            {
                var d = SphericalMercator.Distance(mpC.Y, mpC.X, mpD.Y, mpD.X, 'K') * 100; // distance in km times 10.

                finishPoint.Altitude = 2 + d * d * 0.15;
                //finishPoint.Altitude = Math.Max(res*res*100, 0);
                startPoint.Altitude = 0;
            }

            p.Camera = new Point3D(mpC.X, mpC.Y, finishPoint.Altitude);
            p.Destination = new Point3D(mpD.X, mpD.Y, startPoint.Altitude);
            _lastMes = p.ToString();
        }

        public double Angle(double px1, double py1, double px2, double py2)
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
            if (_rotating)
            {
                _firstMovement = false;

                switch (_rotatingState)
                {
                    case "start":
                        var angles = Angle(startPoint.Mp.X, startPoint.Mp.Y, finishPoint.Mp.X, finishPoint.Mp.Y);
                        var deltaXs = startPoint.Mp.X - finishPoint.Mp.X;
                        var deltaYs = startPoint.Mp.Y - finishPoint.Mp.Y;

                        var distances = Math.Sqrt((deltaXs * deltaXs) + (deltaYs * deltaYs));
                        angles += 1;
                        angles = ((angles - 180) / 360) * 2 * Math.PI;
                        var ys = (int)Math.Round(finishPoint.Mp.Y + distances * Math.Sin(angles));
                        ;
                        var xs = (int)Math.Round(finishPoint.Mp.X + distances * Math.Cos(angles));
                        UpdatePoint("start", new MapPoint(xs, ys));
                        break;
                    case "finish":
                        var angle = Angle(finishPoint.Mp.X, finishPoint.Mp.Y, startPoint.Mp.X, startPoint.Mp.Y);
                        var deltaX = startPoint.Mp.X - finishPoint.Mp.X;
                        var deltaY = startPoint.Mp.Y - finishPoint.Mp.Y;

                        var distance = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
                        angle += 1;
                        angle = ((angle - 180) / 360) * 2 * Math.PI;
                        var y = (int)Math.Round(startPoint.Mp.Y + distance * Math.Sin(angle));
                        ;
                        var x = (int)Math.Round(startPoint.Mp.X + distance * Math.Cos(angle));
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
            _rotating = false;
        }

        internal void StartRotation(string state)
        {
            if (dtr != null && dtr.IsEnabled) dtr.Stop();
            _rotatingState = state;
            _rotating = true;
            dtr = new DispatcherTimer();
            dtr.Interval = new TimeSpan(0, 0, 0, 0, 100);
            dtr.Tick += dtr_Tick;
            dtr.Start();
        }

        internal void Detach(string _state)
        {
            _firstMoveState = null;
            switch (_state)
            {
                case "start":
                    _attachedStart = null;
                    break;
                case "finish":
                    _attachedFinish = null;
                    break;
            }
        }
    }
}