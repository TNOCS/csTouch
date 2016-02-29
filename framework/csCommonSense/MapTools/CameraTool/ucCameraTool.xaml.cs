using csCommon.csMapCustomControls.MapIconMenu;
using csShared;
using csShared.Documents;
using csShared.FloatingElements;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace csGeoLayers.MapTools.CameraTool
{
    public partial class ucCameraTool
    {
        #region fields

        private static readonly WebMercator Mercator = new WebMercator();
        private readonly GroupLayer _layer;
        //private Graphic _attachedGraphic;

        private Camera camera;
        private bool _showInfo;
        private string _state = "start";


        #endregion

        #region dependency properties


        public bool CanAttach
        {
            get { return (bool)GetValue(CanAttachProperty); }
            set { SetValue(CanAttachProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CanAttach.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CanAttachProperty =
            DependencyProperty.Register("CanAttach", typeof(bool), typeof(ucCameraTool), new PropertyMetadata(false));


        // Using a DependencyProperty as the backing store for Position.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register("Position", typeof(MapPoint), typeof(ucCameraTool), new UIPropertyMetadata(null));


        // Using a DependencyProperty as the backing store for Grph.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GrphProperty = DependencyProperty.Register("Grph", typeof(Graphic), typeof(ucCameraTool), new UIPropertyMetadata(null));


        // Using a DependencyProperty as the backing store for LatLon.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LatLonProperty = DependencyProperty.Register("LatLon", typeof(string), typeof(ucCameraTool), new UIPropertyMetadata(""));


        public static readonly DependencyProperty CanBeDraggedProperty = DependencyProperty.Register("CanBeDragged", typeof(bool), typeof(ucCameraTool), new UIPropertyMetadata(false));

        #endregion

        #region constructor

        public ucCameraTool()
        {
            InitializeComponent();
            Loaded += UcPlacemarkLoaded;
            _layer = AppState.ViewDef.MapToolsLayer;
        }

        #endregion

        #region properties

        public string LatLon
        {
            get { return (string)GetValue(LatLonProperty); }
            set { SetValue(LatLonProperty, value); }
        }


        private static AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public bool CanBeDragged
        {
            get { return (bool)GetValue(CanBeDraggedProperty); }
            set { SetValue(CanBeDraggedProperty, value); }
        }

        public bool ShowInfo
        {
            get { return _showInfo; }
            set
            {
                _showInfo = value;
                if (_showInfo && !CanBeDragged)
                {
                    VisualStateManager.GoToState(this, "Info", true);
                }
                else
                {
                    VisualStateManager.GoToState(this, "Icon", true);
                }
            }
        }

        public MapPoint Position
        {
            get { return (MapPoint)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        public Graphic Grph
        {
            get { return (Graphic)GetValue(GrphProperty); }
            set { SetValue(GrphProperty, value); }
        }

        #endregion

        #region private methods

        public static readonly DependencyProperty MenuProperty = DependencyProperty.Register("Menu", typeof(bool), typeof(ucCameraTool), new UIPropertyMetadata(false));
        private DispatcherTimer SliderTimer;

        public bool Menu
        {
            get { return (bool)GetValue(MenuProperty); }
            set { SetValue(MenuProperty, value); }
        }

        private void UcPlacemarkLoaded(object sender, RoutedEventArgs e)
        {
            if (ReferenceEquals(Tag, "First"))
            {
                CanBeDragged = true;
            }
            else
            {
                bCircle.IsExpanded = false;
            }

            ssHeight.ValueChanged += ssHeight_ValueChanged;


            bCircle.Background = Brushes.Black;

            //bCircle.PreviewMouseDown += UcGeorefenceMouseDown;
            //bCircle.PreviewMouseMove += UcGeorefenceMouseMove;
            //bCircle.PreviewMouseUp += UcGeorefenceMouseUp;

            //bCircle.PreviewTouchDown += BCircleTouchDown;
            //bCircle.PreviewTouchMove += BCircleTouchMove;
            //bCircle.PreviewTouchUp += BCircleTouchUp;

            bCircle.RelativeElement = AppState.ViewDef.MapControl;
            bCircle.IconMoved += BCircleIconMoved;
            bCircle.IconTapped += BCircleIconTapped;
            bCircle.IconReleased += BCircleIconReleased;

            var db = DataContext as DataBinding;
            if (db != null)
            {
                camera = db.Attributes["measure"] as Camera;
                _state = db.Attributes["state"].ToString();
                Menu = Convert.ToBoolean(db.Attributes["menuenabled"]);

                if (_state == "finish")
                {
                    iIcon.Source = new BitmapImage(new Uri("pack://application:,,,/csCommon;component/Resources/Icons/camera.png"));
                    StreetView.Visibility = Visibility.Collapsed;
                }

                //switch (db.Attributes["state"].ToString())
                //{
                //    case "StartMeasure":
                //        if (_start != null)
                //        {
                //            Position = (MapPoint)Mercator.ToGeographic(_start.Geometry);
                //            _start.AttributeValueChanged += StartAttributeValueChanged;
                //        }
                //        break;
                //    case "FinishMeasure":
                //        if (_finish != null)
                //        {
                //            Position = (MapPoint)Mercator.ToGeographic(_finish.Geometry);
                //            _finish.AttributeValueChanged += StartAttributeValueChanged;
                //        }
                //        break;

                //}
            }

            if (!CanBeDragged)
            {
                ShowInfo = true;
            }

            if (camera != null)
            {
                sbAutoHeight.IsChecked = camera.AutoHeight;
                camera.PropertyChanged += CameraPropertyChanged;
            }
        }

        private void ssHeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SliderTimer != null && SliderTimer.IsEnabled) SliderTimer.Stop();
            SliderTimer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 0, 3)
            };
            SliderTimer.Tick += SliderTimer_Tick;
            SliderTimer.Start();
            switch (_state)
            {
                case "start":
                    camera.startPoint.Altitude = ssHeight.Value;
                    break;
                case "finish":
                    camera.finishPoint.Altitude = ssHeight.Value;
                    break;
            }
            camera.AutoHeight = false;
            camera.Send3DMessage();
            sbAutoHeight.IsChecked = camera.AutoHeight;
        }

        private void SliderTimer_Tick(object sender, EventArgs e)
        {
            SliderTimer.Stop();
            bHeigth.Visibility = Visibility.Collapsed;
        }

        private void CameraPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            bDetach.Visibility = (Attached)
                                     ? Visibility.Visible
                                     : Visibility.Collapsed;
        }

        #endregion

        #region touch

        private bool _attached;
        private bool _firstMove;
        private Graphic focus;
        private List<DependencyObject> hitResultsList;

        public bool Attached
        {
            get { return _attached; }
            set { _attached = value; }
        }

        private void BCircleIconReleased(object sender, IconMovedEventArgs e)
        {
            if (_firstMove)
            {
                CanBeDragged = true;
                camera = null;
            }
            bCircle.Visibility = (_attached)
                                     ? Visibility.Collapsed
                                     : Visibility.Visible;
            if (focus != null && focus.Geometry is MapPoint && camera != null)
            {
                camera.Attach(_state, focus);
                //bRotateFix.Visibility = Visibility.Visible;
            }
        }

        private void BCircleIconTapped(object sender, IconMovedEventArgs e)
        {
            ShowInfo = !ShowInfo;
        }

        private void GetVisibleGraphics(Rect pos, GroupLayer gl, ref List<Graphic> vg)
        {
            // TODO EV Prevent camera from attaching

            foreach (GroupLayer g in gl.ChildLayers.Where(k => k is GroupLayer))
            {
                GetVisibleGraphics(pos, g, ref vg);
            }
            foreach (GraphicsLayer gr in gl.ChildLayers.Where(k => k is GraphicsLayer))
            {
                if (gr.Visible)
                    foreach (var gc in gr.FindGraphicsInHostCoordinates(pos))
                    {
                        if (gc.Attributes.ContainsKey("Attachable") && gc.Geometry is MapPoint) vg.Add(gc);
                    }
            }
        }

        // Return the result of the hit test to the callback. 
        public HitTestResultBehavior MyHitTestResult(HitTestResult result)
        {
            // Add the hit test result to the list that will be processed after the enumeration.
            hitResultsList.Add(result.VisualHit);

            // Set the behavior to return visuals at all z-order levels. 
            return HitTestResultBehavior.Continue;
        }


        private void BCircleIconMoved(object sender, IconMovedEventArgs e)
        {
            var fe = iIcon;
            hitResultsList = new List<DependencyObject>();
            var pt = fe.TranslatePoint(new Point(0, 0), null);
            var r = new Rect(pt, new Size(fe.ActualWidth / 2, fe.ActualHeight / 2));
            r.X -= fe.ActualWidth / 2;
            r.Y -= fe.ActualHeight / 2;
            if (CanAttach)
            {
                var vg = new List<Graphic>();
                GetVisibleGraphics(r, AppState.ViewDef.Layers, ref vg);
                //var s = vg.OrderBy(k=>SphericalMercator.Distance(k.Geometry as MapPoint, camera.startPoint.Mp))
                focus = vg.FirstOrDefault();
                if (focus != null)
                {
                    //camera.Attach(_state,focus);
                    _attached = true;
                    fe.Opacity = 0.5;
                }
                else
                {
                    _attached = false;
                    fe.Opacity = 1;
                }
            }

            // Set up a callback to receive the hit test result enumeration, 
            // but no hit test filter enumeration.
            //VisualTreeHelper.HitTest(Application.Current.MainWindow,
            //                  null,  // No hit test filtering. 
            //                  new HitTestResultCallback(MyHitTestResult),
            //                  new PointHitTestParameters(pt));


            if (camera == null)
            {
                _firstMove = true;
            }
            else
            {
                camera.StopRotation();
            }
            if (CanBeDragged && camera == null)
            {
                camera = new Camera
                {
                    Layer = _layer
                };

                var p = e.Position;
                var pos = AppState.ViewDef.ViewToWorld(p.X, p.Y);
                var mpStart = (MapPoint)Mercator.FromGeographic(new MapPoint(pos.Y, pos.X));

                var p2 = e.Position;
                var pos2 = AppState.ViewDef.ViewToWorld(p2.X + 65, p2.Y - 65);
                var mpFinish = (MapPoint)Mercator.FromGeographic(new MapPoint(pos2.Y, pos2.X));

                camera.Init(_layer, mpStart, mpFinish, Resources);

                CanBeDragged = false;

                //e.TouchDevice.Capture(bCircle);
                //e.Handled = true;
            }
            else
            {
                if (camera != null)
                {
                    var pos = e.Position;
                    var w = AppState.ViewDef.ViewToWorld(pos.X, pos.Y);
                    camera.UpdatePoint(_state, (MapPoint)Mercator.FromGeographic(new MapPoint(w.Y, w.X)));

                    //_center = e.GetTouchPoint(AppState.MapControl).Position;
                    //e.TouchDevice.Capture(bCircle);
                    //e.Handled = true;
                }
            }
        }

        #endregion

        #region mouse

        #endregion

        private void MapMenuItemTap(object sender, RoutedEventArgs e)
        {
            camera.Remove();
        }

        private void AttachTap(object sender, RoutedEventArgs e)
        {
            CanAttach = !CanAttach;
            bAttachButton.Background = (CanAttach) ? Brushes.Black : Brushes.Gray;
        }

        private void HeightTap(object sender, RoutedEventArgs e)
        {
            bCircle.IsExpanded = false;
            bHeigth.Visibility = Visibility.Visible;
            //camera.StartRotation(_state);
        }

        public static double Angle(double px1, double py1, double px2, double py2)
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

        private void StreetViewTap(object sender, RoutedEventArgs e)
        {
            var wm = new WebMercator();
            MapPoint mp = null;
            switch (_state)
            {
                case "start":
                    mp = camera.startPoint.Mp;
                    break;
                case "finish":
                    mp = camera.finishPoint.Mp;
                    break;
            }
            var m = wm.ToGeographic(mp) as MapPoint;
            var angle = (int)Angle(camera.startPoint.Mp.X, camera.startPoint.Mp.Y, camera.finishPoint.Mp.X, camera.finishPoint.Mp.Y);
            var l = m.Y.ToString(CultureInfo.InvariantCulture) + "," + m.X.ToString(CultureInfo.InvariantCulture);
            var url = "http://maps.googleapis.com/maps/api/streetview?size=640x480&location=" + l + "&heading=" + angle + "&fov=90&pitch=0&sensor=false";
            var fe = FloatingHelpers.CreateFloatingElement(new Document
            {
                Location = url,
                OriginalUrl = url,
                FileType = FileTypes.image,
                ShareUrl = url,
            });
            AppState.FloatingItems.AddFloatingElement(fe);
        }

        private void bDetach_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            Detach();
        }

        private void bDetach_TouchDown_1(object sender, TouchEventArgs e)
        {
            Detach();
        }

        private void Detach()
        {
            bRotateFix.Visibility = Visibility.Collapsed;
            camera.Detach(_state);
            Attached = false;
            bCircle.Visibility = Visibility.Visible;
        }

        private void sbAutoHeight_Click(object sender, RoutedEventArgs e)
        {
            camera.AutoHeight = true;
            camera.Send3DMessage();
            sbAutoHeight.IsChecked = camera.AutoHeight;
            bHeigth.Visibility = Visibility.Collapsed;
        }

        private void bRotateFix_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            camera.FixRotation(_state);
        }

        private void bRotateFix_TouchDown(object sender, System.Windows.Input.TouchEventArgs e)
        {
            camera.FixRotation(_state);
        }

        private void ssHeight_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //bHeigth.Visibility = Visibility.Collapsed;
        }

        private void ssHeight_TouchUp(object sender, TouchEventArgs e)
        {
            //bHeigth.Visibility = Visibility.Collapsed;
        }
    }

    public class csPoint
    {
        public bool Fixed = false;
        public MapPoint Mp;

        private double altitude = double.NaN;

        public double Altitude
        {
            get { return altitude; }
            set { altitude = value; }
        }
    }
}