#region references

using csCommon.csMapCustomControls.MapIconMenu;
using csCommon.MapPlugins.MapTools.MeasureTool;
using csShared;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using System;
using System.Windows;
using System.Windows.Media;

#endregion

namespace csGeoLayers.MapTools.MeasureTool
{
    public partial class ucMeasureMapTool
    {

        #region fields

        private static readonly WebMercator Mercator = new WebMercator();
        private bool _showInfo;
        private GroupLayer _layer;

        private Measure _measure;
        private string _state = "start";

        #endregion

        #region dependency properties

        // Using a DependencyProperty as the backing store for Position.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register("Position", typeof(MapPoint), typeof(ucMeasureMapTool),
                                        new UIPropertyMetadata(null));


        // Using a DependencyProperty as the backing store for Grph.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GrphProperty =
            DependencyProperty.Register("Grph", typeof(Graphic), typeof(ucMeasureMapTool), new UIPropertyMetadata(null));


        // Using a DependencyProperty as the backing store for LatLon.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LatLonProperty =
            DependencyProperty.Register("LatLon", typeof(string), typeof(ucMeasureMapTool), new UIPropertyMetadata(""));


        public static readonly DependencyProperty CanBeDraggedProperty =
            DependencyProperty.Register("CanBeDragged", typeof(bool), typeof(ucMeasureMapTool),
                                        new UIPropertyMetadata(false));



        #endregion

        #region constructor

        public ucMeasureMapTool()
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

        private void UcPlacemarkLoaded(object sender, RoutedEventArgs e)
        {

            if (ReferenceEquals(Tag, "First"))
            {
                CanBeDragged = true;
            }
            else
            {
                bCircle.IsExpanded = true;

            }




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


            if (DataContext is DataBinding)
            {
                var db = (DataBinding)DataContext;
                this._measure = db.Attributes["measure"] as Measure;
                _state = db.Attributes["state"].ToString();
                _measure.PropertyChanged += _measure_PropertyChanged;

                AppState.ViewDef.MapManipulationDelta += ViewDef_MapManipulationDelta;

                Menu = Convert.ToBoolean(db.Attributes["menuenabled"]);

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
        }

        public bool Menu
        {
            get { return (bool)GetValue(MenuProperty); }
            set { SetValue(MenuProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Menu.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MenuProperty =
            DependencyProperty.Register("Menu", typeof(bool), typeof(ucMeasureMapTool), new UIPropertyMetadata(false));



        void ViewDef_MapManipulationDelta(object sender, EventArgs e)
        {
            UpdateDistance();
        }

        void _measure_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Distance")
            {
                UpdateDistance();
            }
        }


        #endregion

        #region touch

        private bool _firstMove;


        private void BCircleIconReleased(object sender, IconMovedEventArgs e)
        {
            if (_firstMove)
            {
                CanBeDragged = true;
                _measure = null;
            }
        }

        private void BCircleIconTapped(object sender, IconMovedEventArgs e)
        {
            ShowInfo = !ShowInfo;
        }

        private void BCircleIconMoved(object sender, IconMovedEventArgs e)
        {
            if (_measure == null) _firstMove = true;
            if (CanBeDragged && _measure == null)
            {
                _measure = new Measure() { Layer = this._layer };

                Point p = e.Position;
                SharpMap.Geometries.Point pos = AppState.ViewDef.ViewToWorld(p.X, p.Y);
                MapPoint mpStart = (MapPoint)Mercator.FromGeographic(new MapPoint(pos.Y, pos.X));

                Point p2 = e.Position;
                SharpMap.Geometries.Point pos2 = AppState.ViewDef.ViewToWorld(p2.X + 65, p2.Y - 65);
                MapPoint mpFinish = (MapPoint)Mercator.FromGeographic(new MapPoint(pos2.Y, pos2.X));

                _measure.Init(_layer, mpStart, mpFinish, this.Resources);




                CanBeDragged = false;

                //e.TouchDevice.Capture(bCircle);
                //e.Handled = true;
            }
            else
            {


                if (_measure != null)
                {
                    Point pos = e.Position;
                    SharpMap.Geometries.Point w = AppState.ViewDef.ViewToWorld(pos.X, pos.Y);
                    _measure.UpdatePoint(_state, (MapPoint)Mercator.FromGeographic(new MapPoint(w.Y, w.X)));
                    UpdateDistance();
                    //_center = e.GetTouchPoint(AppState.MapControl).Position;
                    //e.TouchDevice.Capture(bCircle);
                    //e.Handled = true;

                }
            }
        }



        #endregion

        #region mouse


        public double Angle(double px1, double py1, double px2, double py2)
        {
            // Negate X and Y values
            double pxRes = px2 - px1;
            double pyRes = py2 - py1;
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
                if (pxRes > 0.0)
                    angle = 0.0;
                else
                    angle = Math.PI;
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

        private void UpdateDistance()
        {
            if (this._state == "start")
            {
                double d = _measure.Distance;
                tDistance.Text = d.ToString("#####.##") + " km";
                var s = AppState.ViewDef.MapPoint(_measure.Start.Mp);
                var f = AppState.ViewDef.MapPoint(_measure.Finish.Mp);
                var x = ((f.X - s.X) / 2) + s.X;
                var y = ((f.Y - s.Y) / 2) + s.Y;
                Point p = new Point(x, y);
                Point rel = AppState.ViewDef.MapControl.TranslatePoint(p, this);
                TransformGroup tg = new TransformGroup();
                tg.Children.Add(new TranslateTransform(rel.X, rel.Y));
                double a = Angle(s.X, s.Y, f.X, f.Y);


                tbDistance.RenderTransform = new RotateTransform(a);
                tbDistance.RenderTransformOrigin = new Point(0.5, 0.5);
                bDistance.RenderTransform = tg;
                bDistance.Visibility = Visibility.Visible;

            }
            else
            {
                bDistance.Visibility = Visibility.Collapsed;
            }
        }


        #endregion

        private void MapMenuItem_Tap(object sender, RoutedEventArgs e)
        {
            _measure.Remove();
        }
    }

    public class csPoint
    {
        public MapPoint Mp;
        public bool Fixed = false;
    }
}