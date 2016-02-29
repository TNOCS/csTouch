#region references

using csCommon.csMapCustomControls.MapIconMenu;
using csShared;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using System;
using System.Windows;
using System.Windows.Media;
using Humanizer;
using OxyPlot;

#endregion

namespace csCommon.MapPlugins.MapTools.RouteTool
{
    public partial class ucRouteTool
    {

        #region fields

        private static readonly WebMercator Mercator = new WebMercator();
        private bool showInfo;
        private readonly GroupLayer layer;

        private Route measure;
        private string state = "start";

        #endregion

        #region dependency properties

        // Using a DependencyProperty as the backing store for Position.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register("Position", typeof(MapPoint), typeof(ucRouteTool), new UIPropertyMetadata(null));


        // Using a DependencyProperty as the backing store for Grph.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GrphProperty =
            DependencyProperty.Register("Grph", typeof(Graphic), typeof(ucRouteTool), new UIPropertyMetadata(null));


        // Using a DependencyProperty as the backing store for LatLon.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LatLonProperty =
            DependencyProperty.Register("LatLon", typeof(string), typeof(ucRouteTool), new UIPropertyMetadata(""));


        public static readonly DependencyProperty CanBeDraggedProperty =
            DependencyProperty.Register("CanBeDragged", typeof(bool), typeof(ucRouteTool), new UIPropertyMetadata(false));



        #endregion

        #region constructor

        public ucRouteTool()
        {
            InitializeComponent();
            Loaded += UcPlacemarkLoaded;
            layer = AppState.ViewDef.MapToolsLayer;
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
            get { return showInfo; }
            set
            {
                showInfo = value;
                if (showInfo && !CanBeDragged)
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

            var binding = DataContext as DataBinding;
            if (binding != null)
            {
                var db = binding;
                measure = db.Attributes["measure"] as Route;
                state = db.Attributes["state"].ToString();
                if (measure != null) measure.PropertyChanged += _measure_PropertyChanged;

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
            DependencyProperty.Register("Menu", typeof(bool), typeof(ucRouteTool), new UIPropertyMetadata(false));



        void ViewDef_MapManipulationDelta(object sender, EventArgs e)
        {
            UpdateDistance();
        }

        void _measure_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Directions")
            {
                UpdateDistance();
            }
        }


        #endregion

        #region touch

        private bool firstMove;


        private void BCircleIconReleased(object sender, IconMovedEventArgs e)
        {
            if (!firstMove) return;
            CanBeDragged = true;
            measure = null;
        }

        private void BCircleIconTapped(object sender, IconMovedEventArgs e)
        {
            ShowInfo = !ShowInfo;
        }

        private void BCircleIconMoved(object sender, IconMovedEventArgs e)
        {
            if (measure == null) firstMove = true;
            if (CanBeDragged && measure == null)
            {
                measure = new Route { Layer = layer, Mode = "driving" };

                var p = e.Position;
                var pos = AppState.ViewDef.ViewToWorld(p.X, p.Y);
                var mpStart = (MapPoint)Mercator.FromGeographic(new MapPoint(pos.Y, pos.X));

                var p2 = e.Position;
                var pos2 = AppState.ViewDef.ViewToWorld(p2.X + 65, p2.Y - 65);
                var mpFinish = (MapPoint)Mercator.FromGeographic(new MapPoint(pos2.Y, pos2.X));

                measure.Init(layer, mpStart, mpFinish, Resources);

                CanBeDragged = false;
                //e.TouchDevice.Capture(bCircle);
                //e.Handled = true;
            }
            else
            {
                if (measure == null) return;
                var pos = e.Position;
                var w = AppState.ViewDef.ViewToWorld(pos.X, pos.Y);
                measure.UpdatePoint(state, (MapPoint)Mercator.FromGeographic(new MapPoint(w.Y, w.X)));
                UpdateDistance();
                //_center = e.GetTouchPoint(AppState.MapControl).Position;
                //e.TouchDevice.Capture(bCircle);
                //e.Handled = true;
            }
        }



        #endregion

        #region mouse


        public double Angle(double px1, double py1, double px2, double py2)
        {
            // Negate X and Y values
            var pxRes = px2 - px1;
            var pyRes = py2 - py1;
            double angle;
            // Calculate the angle
            if (pxRes.IsZero())
            {
                if (pxRes.IsZero())
                    angle = 0.0;
                else if (pyRes > 0.0) angle = Math.PI / 2.0;
                else
                    angle = Math.PI * 3.0 / 2.0;
            }
            else if (pyRes.IsZero())
            {
                angle = pxRes > 0.0 ? 0.0 : Math.PI;
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
            if (state == "start")
            {
                if (measure.Directions == null || measure.Directions.Directions == null) return;
                border.Visibility = Visibility.Visible;

                path.Visibility = Visibility.Visible;
                spAddress.DataContext = measure.Directions;

                tbDistance.Text = (Convert.ToInt32(measure.Directions.Directions.Distance.meters) / 1000.0).ToString("###.##") + " km";
                long seconds;
                if (long.TryParse(measure.Directions.Directions.Duration.seconds, out seconds))
                    tbDuration.Text = TimeSpan.FromSeconds(seconds).Humanize(seconds > 3600 ? 2 : 1);
            }
            else
            {
                border.Visibility = Visibility.Collapsed;
                path.Visibility = Visibility.Collapsed;
            }
        }


        #endregion

        private void MapMenuItem_Tap(object sender, RoutedEventArgs e)
        {
            measure.Remove();
        }

        private void mmiZoom_Tap(object sender, RoutedEventArgs e)
        {
            AppStateSettings.Instance.ViewDef.MapControl.ZoomDuration = new TimeSpan(0, 0, 0, 1);
            AppStateSettings.Instance.ViewDef.MapControl.ZoomToResolution(1, state == "start" 
                ? measure.Start.Mp 
                : measure.Finish.Mp);
        }

        private void mmiPlay_Tap(object sender, RoutedEventArgs e)
        {
            measure.StartPlay();
        }
    }


}