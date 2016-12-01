using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Caliburn.Micro;
using csCommon.csMapCustomControls.MapIconMenu;
using DataServer;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using Microsoft.Surface.Presentation.Input;
using csShared;
using csShared.Geo;
using csShared.Utils;

namespace csDataServerPlugin
{
    /// <summary>
    /// Interaction logic for ucPoi.xaml
    /// </summary>
    public partial class ucPoiIcon
    {
        private const double Rad2Deg = 180.0/Math.PI;

        private DateTime dt = DateTime.Now;
        private bool rotating;
        private double lastRes;

        public ucPoiIcon()
        {
            InitializeComponent();
            Loaded += UcPoiIconLoaded;
            AppStateSettings.Instance.ViewDef.MapControl.ExtentChanged += MapControlExtentChanged;
        }

        private void MapControlExtentChanged(object sender, ExtentEventArgs e)
        {
            if (Math.Abs(lastRes - AppStateSettings.Instance.ViewDef.MapControl.Resolution) < 0.01) return;
            UpdateVisibilityOfTitle();
            lastRes = AppStateSettings.Instance.ViewDef.MapControl.Resolution;
        }

        public static AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public PoI PoI
        {
            get { return (PoI)GetValue(PoIProperty); }
            set { SetValue(PoIProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PoI.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PoIProperty =
            DependencyProperty.Register("PoI", typeof(PoI), typeof(ucPoiIcon), new PropertyMetadata(null));        

        public PoiGraphic Graphic
        {
            get { return (PoiGraphic)GetValue(GraphicProperty); }
            set { SetValue(GraphicProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Graphic.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GraphicProperty =
            DependencyProperty.Register("Graphic", typeof(PoiGraphic), typeof(ucPoiIcon), new PropertyMetadata(null));        

        // Using a DependencyProperty as the backing store for PoI.  This enables animation, styling, binding, etc...
        public DataServerPlugin Plugin
        {
            get { return Graphic.Plugin; }            
        }

        
        public IServiceLayer Layer
        {
            get { return Graphic.GroupLayer; }
        }

        // Using a DependencyProperty as the backing store for Layer.  This enables animation, styling, binding, etc...


        private void UcPoiIconLoaded(object sender, RoutedEventArgs e)
        {
            //var s = DataContext;
            if (Graphic == null || Graphic.Poi == null || !(Graphic.Poi is PoI)) return;
            PoI = (PoI)Graphic.Poi;
            PoI.PropertyChanged += PoI_PropertyChanged;
            PoI.NEffectiveStyle.PropertyChanged += (s, f) =>
            {
                if (f.PropertyName == "Picture")
                {
                    Execute.OnUIThread(() =>
                    {
                        iPoiIcon.GetBindingExpression(Image.SourceProperty).UpdateTarget();
                    });
                }
            };
            //if (PoI.PoiType != null && PoI.PoiType.Style != null)
            //{
            //    PoI.PoiType.Style.StyleChanged += (s, f) =>
            //    {
            //    };
            //}
            PoI.RotationStarted += PoI_RotationStarted;
            bCircle.RelativeElement = AppStateSettings.Instance.ViewDef.MapControl;
            //miRemove.Visibility = PoI.CanDelete
            //                          ? Visibility.Visible
            //                          : Visibility.Collapsed;
            //miRotate.Visibility = PoI.EffectiveStyle.CanRotate
            //                          ? Visibility.Visible
            //                          : Visibility.Collapsed;

            //MetaLabels = Layer.GetMetaLabels(this.PoI);
            cHeading.MouseDown += CalcHeadingMouse;
            cHeading.MouseMove += CalcHeadingMouse;
            cHeading.TouchDown += CalcHeading;
            cHeading.TouchMove += CalcHeading;
            cHeading.MouseUp += CHeadingMouseUp;
            cHeading.TouchUp += CHeadingTouchUp;
            //bCircle.Tap += BCircleTap;

            UpdateHeading();

            UpdateVisibilityOfTitle();

            try
            {
                if (PoI.DrawingMode == DrawingModes.Image)
                {
                    bIcon.Background = null;
                    bIcon.Width = 30;
                    bIcon.Height = 30;
                }
                else
                {
                    bIcon.Background = new SolidColorBrush(PoI.NEffectiveStyle.FillColor.Value);
                }

                PoI.PropertyChanged += PoIPropertyChanged;
            }
            catch (SystemException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        void PoI_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "NEffectiveStyle" && bIcon.Background != null)
            {
                Execute.OnUIThread(() =>
                {
                    if (bIcon.Background.ToString() != new SolidColorBrush(PoI.NEffectiveStyle.FillColor.Value).ToString())
                        bIcon.Background = new SolidColorBrush(PoI.NEffectiveStyle.FillColor.Value);
                });
                
            }
        }

        private void UpdateVisibilityOfTitle()
        {
            if (PoI == null) return;
            tbName.Visibility = (PoI.NEffectiveStyle.TitleMode == TitleModes.None ||
                                 PoI.NEffectiveStyle.MaxTitleResolution < AppStateSettings.Instance.ViewDef.MapControl.Resolution)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        void PoI_RotationStarted(object sender, EventArgs e)
        {
            Dispatcher.Invoke(ToggleRotating);
        }


        private void PoIPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Orientation") return;
            UpdateHeading();
            if (dt.AddMilliseconds(500) < DateTime.Now)
            {                    
                dt = DateTime.Now;
            }
        }

        /// <summary>
        /// Calculate angle in radians between line defined with two points and x-axis.
        /// </summary>
        private static double Angle(Point start, Point end)
        {
            return Math.Atan2(start.Y - end.Y, end.X - start.X)*Rad2Deg;
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
                else if (pyRes > 0.0) angle = Math.PI/2.0;
                else
                    angle = Math.PI*3.0/2.0;
            }
            else if (pyRes == 0.0)
            {
                angle = pxRes > 0.0 ? 0.0 : Math.PI;
            }
            else
            {
                if (pxRes < 0.0)
                    angle = Math.Atan(pyRes/pxRes) + Math.PI;
                else if (pyRes < 0.0) angle = Math.Atan(pyRes/pxRes) + (2*Math.PI);
                else
                    angle = Math.Atan(pyRes/pxRes);
            }
            // Convert to degrees
            angle = angle*180/Math.PI;
            return angle;
        }


        private void CalcHeadingMouse(object e, MouseEventArgs a)
        {
            if (PoI == null) return;
            try
            {
                if (a.MouseDevice.LeftButton == MouseButtonState.Pressed)
                {
                    InputDevice id = a.MouseDevice;

                    id.Capture(cHeading);

                    var c1 = AppStateSettings.Instance.ViewDef.MapPoint(new KmlPoint(PoI.Position.Longitude, PoI.Position.Latitude));
                    var c2 = a.GetPosition(AppStateSettings.Instance.ViewDef.Map);

                    var angle = 450 - Angle(c1, c2);

                    angle = angle%360;

                    PoI.Orientation = (float) angle;
                    //UpdateHeading();
                    a.Handled = true;
                }
                else
                {
                    cHeading.ReleaseAllCaptures();
                }
            }
            catch (SystemException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void CHeadingTouchUp(object sender, TouchEventArgs e)
        {
            FinishHeading();
        }

        private void CHeadingMouseUp(object sender, MouseButtonEventArgs e)
        {
            FinishHeading();
        }

        private void FinishHeading()
        {
            PoI.Service.TriggerContentChanged(PoI);
            ToggleRotating();
        }

        private void CalcHeading(object e, TouchEventArgs a)
        {
            InputDevice id = a.TouchDevice;

            id.Capture(cHeading);

            var c1 = AppStateSettings.Instance.ViewDef.MapPoint(new KmlPoint(PoI.Position.Longitude, PoI.Position.Latitude));
            // _startthis.PointToScreen(new Point(0, 0));
            // this.TranslatePoint(new Point(0, 0), Application.Current.MainWindow);
            var c2 = a.GetTouchPoint(AppStateSettings.Instance.ViewDef.Map).Position;
            var angle = 450 - Angle(c1, c2);
            
            angle = angle%360;
            PoI.Orientation = (float) angle;
            UpdateHeading();
            a.Handled = true;
        }


        //private void BCircleTap(object sender, RoutedEventArgs e)
        //{
           
        //}


        private void BCircleIconMoved(object sender, IconMovedEventArgs e)
        {
            UpdatePosition(e);
        }

        public void UpdatePosition(IconMovedEventArgs e)
        {
            try
            {
                if (PoI == null || !PoI.NEffectiveStyle.CanMove.Value) return;
                var s = AppState.ViewDef.ViewToWorld(e.Position.X, e.Position.Y);
                PoI.Position = new Position(s.Y, s.X);
            }
            catch (Exception et)
            {
                Logger.Log("Poi Update", "Error positioning PoI", et.Message, Logger.Level.Error);
            }
        }

        private void MiRemoveTap(object sender, RoutedEventArgs e)
        {
            if (Layer == null) return;
            if (Layer.Service.PoIs.Contains(PoI)) 
            Layer.Service.RemovePoi(PoI);
        }

        private void BCircleIconTapped(object sender, IconMovedEventArgs e)
        {
            if (Graphic == null) return;
            //PoI.EffectiveStyle.CallOutFillColor = PoI.EffectiveStyle.CallOutFillColor;
            Graphic.TappedByExternalMapControlMapGesture(new MapPoint(e.Position.X,e.Position.Y));
            //todo if (Layer.DataService.ClickBehaviour == PoIClickBehaviour.popup)
            //{
                //Layer.OpenPoiPopup(this.PoI);                
            //}
        }

        private void BCircleIconReleased(object sender, IconMovedEventArgs e)
        {
            //todo Layer.CheckDirty();
            // TODO: Add event handler implementation here.
        }

        private void UpdateHeading() {
            Dispatcher.Invoke(delegate {
                if (PoI == null) return;
                try {
                    var centerPoint = new Point(88.5, 88.5);
                    const int distance = 100;
                    const double twoPi = Math.PI*2;
                    var x = centerPoint.Y + (int) Math.Round(distance*Math.Sin(((PoI.Orientation - 90)/360)*twoPi));
                    var y = centerPoint.X + (int) Math.Round(distance*Math.Cos(((PoI.Orientation - 90)/360)*twoPi));
                    ePos.SetValue(Canvas.TopProperty, x);
                    ePos.SetValue(Canvas.LeftProperty, y);

                    //Bearing = SystemInstance.Bearing;
                    var transformGroup = bIcon.RenderTransform as TransformGroup;
                    if (transformGroup == null) return;
                    var rotateTransform = transformGroup.Children[0] as RotateTransform;
                    if (rotateTransform != null)
                        rotateTransform.Angle = PoI.Orientation - 90;
                }
                catch (SystemException e) {
                    Console.WriteLine(e.Message);
                }
            });
        }

        private void MiRotateTap1(object sender, RoutedEventArgs e)
        {
            ToggleRotating();
            
        }

        private DateTime lastToggle = DateTime.Now;

        public void ToggleRotating()
        {
            if (lastToggle.AddMilliseconds(100) >= DateTime.Now) return;
            rotating = !rotating;
            //miRotate.IsSelected = rotating;
            rScale.ScaleX = (rotating) ? 1 : 0;
            rScale.ScaleY = (rotating) ? 1 : 0;
            //VisualStateManager.GoToState(this, (rotating)
            //                                       ? "Rotating"
            //                                       : "Default", true);
            UpdateHeading();
            lastToggle = DateTime.Now;
        }

        private void MiZoomTap(object sender, RoutedEventArgs e)
        {
            var wm = new WebMercator();
            AppStateSettings.Instance.ViewDef.MapControl.ZoomDuration = new TimeSpan(0, 0, 0, 1);
            AppStateSettings.Instance.ViewDef.MapControl.ZoomToResolution(1, (MapPoint) wm.FromGeographic(new MapPoint(PoI.Position.Longitude, PoI.Position.Latitude)));
        }
         
        private void border_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //TODO if (Layer.DataService.ClickBehaviour == PoIClickBehaviour.calloutpopup)
            {
                //Layer.OpenPoiPopup(this.PoI);
                e.Handled = true;
            }
            // TODO: Add event handler implementation here.
        }

        private void MiEvent_OnTap(object sender, RoutedEventArgs e)
        {
            //todo event
            //EventBase eb = new EventBase();
            //eb.Id = Guid.NewGuid();
            //eb.ShowOnTimeline = true;
            //eb.ShowInList = true;
            //eb.Latitude = PoI.Position.Latitude;
            //eb.Longitude = PoI.Position.Longitude;
            //eb.Date = AppState.TimelineManager.FocusTime;
            //eb.Name = "test";
            //Layer.Service.Events.Add(eb);
        }

        /// <summary>
        /// When PoI is pressed for long time
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BCircleIconLongTapped(object sender, EventArgs e)
        {
            if ((PoI != null) && (PoI.Service is PoiService)) (PoI.Service as PoiService).RaisePoiLongTapped(PoI);
        }

        private void BCircleIconRightClicked(object sender, EventArgs e)
        {
            if ((PoI != null) && (PoI.Service is PoiService)) (PoI.Service as PoiService).RaisePoiRightClicked(PoI);
        }

    }
}