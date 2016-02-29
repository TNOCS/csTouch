using Caliburn.Micro;
using csEvents;
using csShared;
using csShared.Controls.Popups.MenuPopup;
using csShared.Timeline;
using csTimeTabPlugin;
using ESRI.ArcGIS.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Input.Manipulations;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

//using Microsoft.Surface.Presentation.Manipulations;

namespace csCommon.Plugins.Timeline
{
    /// <summary>
    ///     Interaction logic for ucTimeline.xaml
    /// </summary>
    public partial class TimelineView
    {
        #region properties

        // Using a DependencyProperty as the backing store for BackgroundColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof(Brush), typeof(TimelineView),
                new UIPropertyMetadata(Brushes.White));

        // Using a DependencyProperty as the backing store for DividerColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DividerColorProperty =
            DependencyProperty.Register("DividerColor", typeof(Brush), typeof(TimelineView),
                new UIPropertyMetadata(Brushes.White));

        // Using a DependencyProperty as the backing store for TextColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextColorProperty =
            DependencyProperty.Register("TextColor", typeof(Brush), typeof(TimelineView),
                new UIPropertyMetadata(Brushes.White));

        // Using a DependencyProperty as the backing store for DividerWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DividerWidthProperty =
            DependencyProperty.Register("DividerWidth", typeof(double), typeof(TimelineView), new UIPropertyMetadata(2.0d));

        // Using a DependencyProperty as the backing store for HasFocus.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HasFocusProperty =
            DependencyProperty.Register("HasFocus", typeof(bool), typeof(TimelineView), new UIPropertyMetadata(true));

        // Using a DependencyProperty as the backing store for HasFocus.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FutureBrushProperty =
            DependencyProperty.Register("FutureBrush", typeof(Brush), typeof(TimelineView), new UIPropertyMetadata(AppStateSettings.Instance.AccentBrush));

        public Line LNow;

        private DispatcherTimer nowTimer;
        private bool timeUpdated;
        private bool busy;
        private bool fixHold;
        private double delta;
        private double span = 24;

        public static TimelineView TimelineViewInstance { get; set; }


        public Dictionary<string, FrameworkElement> ElementCache { get; set; }

        private static readonly AppStateSettings AppState = AppStateSettings.Instance;

        public TimelineManager Timeline
        {
            get { return AppState.TimelineManager; }
        }

        public Brush BackgroundColor
        {
            get { return (Brush)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }

        public Brush DividerColor
        {
            get { return (Brush)GetValue(DividerColorProperty); }
            set { SetValue(DividerColorProperty, value); }
        }

        public Brush TextColor
        {
            get { return (Brush)GetValue(TextColorProperty); }
            set { SetValue(TextColorProperty, value); }
        }

        public double DividerWidth
        {
            get { return (double)GetValue(DividerWidthProperty); }
            set { SetValue(DividerWidthProperty, value); }
        }

        public bool HasFocus
        {
            get { return (bool)GetValue(HasFocusProperty); }
            set { SetValue(HasFocusProperty, value); }
        }

        public Brush FutureBrush
        {
            get { return (Brush)GetValue(FutureBrushProperty); }
            set { SetValue(FutureBrushProperty, value); }
        }

        #endregion

        #region timeline

        // 100 rotations/second squared, specified in radians (200 pi / (1000ms/s)^2)

        // 24 inches/second squared (24 inches * 96 pixels per inch / (1000ms/s )^2)
        private const double Deceleration = 24.0 * 96.0 / (1000.0 * 1000.0);

        // When inertia delta values get scaled by more than this amount, stop the inertia early
        private InertiaProcessor2D inertiaProcessorTimeline; // BUG TODO This variable is never assigned, but it is used in code!
        private DispatcherTimer inertiaTimelineTimer;
        //private ManipulationProcessor2D manipulationProcessorTimeline;

        // The following variables help us remember the angular velocity the fractal
        // has when the second contact is lost, allowing rotational inertia where there are less 
        // than 2 contacts touching the fractal.

        private bool isMouseDown;
        private Point lastMousePos;

        private bool isMouseDownBm;
        private Point lastMousePosBm;

        private bool stopInertia;

        /// <summary>
        ///     Gets the current timestamp.
        /// </summary>
        private static long Timestamp
        {
            get
            {
                // The question of what tick source to use is a difficult
                // one in general, but for purposes of this test app,
                // DateTime ticks are good enough.
                return DateTime.UtcNow.Ticks;
            }
        }

        #endregion

        public TimelineView()
        {
            InitializeComponent();
            Loaded += UcTimelineLoaded;
            Unloaded += UcTimelineUnloaded;
            FutureBrush = AppStateSettings.Instance.AccentBrush;
            ElementCache = new Dictionary<string, FrameworkElement>();
            TimelineViewInstance = this;
        }

        //private readonly List<TimelineItem> timelineItems = new List<TimelineItem>();

        public void AddItemToTimeline(TimelineItem tlItem)
        {
            //if (timelineItems.Contains(tlItem)) return;
            //timelineItems.Add(tlItem);
            //Draw();
        }

        public void RemoveItemFromTimeline(TimelineItem tlItem)
        {
            //if (!timelineItems.Contains(tlItem)) return;
            //timelineItems.Remove(tlItem);
            //Draw();
        }

        private bool IsFixClose()
        {
            double diff = Math.Abs(FindPos(Timeline.CurrentTime) - FindPos(Timeline.FocusTime));
            return diff < 20;
        }

        private void TimelineTimeChanged(object sender, EventArgs e)
        {
            Execute.OnUIThread(() =>
            {
                timeUpdated = true;
                Timeline.TimelinePlayer.FixFocus = fixHold && !isMouseDownBm;
                AppStateSettings.Instance.ViewDef.MapControl.TimeExtent = new TimeExtent(Timeline.Start, Timeline.End);
                if (!fixHold)
                {
                    rFixed.Opacity = (Timeline.TimelinePlayer.FixFocus) ? 100 : 0;
                    //Dispatcher.Invoke(delegate { rFixed.Opacity = (Timeline.TimelinePlayer.FixFocus) ? 100 : 0; },DispatcherPriority.Background);
                }

                UpdateCurrentTime();
            });

        }

        private void UcTimelineUnloaded(object sender, RoutedEventArgs e)
        {
            if (nowTimer != null && nowTimer.IsEnabled) nowTimer.Stop();
        }

        private void NowTimerTick(object sender, EventArgs e)
        {
            UpdateCurrentTime();
        }

        [DebuggerStepThrough]
        private void CompositionTargetRendering(object sender, EventArgs e)
        {
            //check if map was updated
            if (!timeUpdated || Visibility != Visibility.Visible || DesignerProperties.GetIsInDesignMode(this)) return;
            //CalculateFocusTime();
            Draw();
            timeUpdated = false;
        }


        private void UpdateVisibility()
        {
            LayoutRoot.Visibility = AppState.TimelineManager.Visible ? Visibility.Visible : Visibility.Collapsed;

            Height = AppStateSettings.Instance.Config.GetDouble("Timeline.Height", 130);
            var eventsmargin = AppStateSettings.Instance.Config.GetDouble("Timeline.EventsHeight", 30);
            cEvents.Margin = new Thickness(0, 0, 0, Height - eventsmargin);
            cEvents.Height = Timeline.EventsVisible ? eventsmargin : 0;
            cTimeLine.Margin = Timeline.EventsVisible ? new Thickness(0, 0, 0, 0) : new Thickness(0);
            cTimeLine.Height = Timeline.EventsVisible ? Height - eventsmargin : Height;
            LayoutRoot.Height = Height;
            VerticalAlignment = VerticalAlignment.Bottom;
            bFocusTime.Visibility = Timeline.FocusVisible ? Visibility.Visible : Visibility.Collapsed;
            cEvents.Visibility = Timeline.EventsVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UcTimelineLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this)) return;
            AppState.TimelineManager.VisibilityChanged += (s, et) => UpdateVisibility();
            UpdateVisibility();

            cTimeLine.IsManipulationEnabled = true;
            bFocusTime.IsManipulationEnabled = true;

            #region event handlers

            cTimeLine.ManipulationStarting += cTimeLine_ManipulationStarting;
            cTimeLine.ManipulationDelta += cTimeLine_ManipulationDelta;
            cTimeLine.ManipulationInertiaStarting += cTimeLine_ManipulationInertiaStarting;
            cTimeLine.ManipulationCompleted += cTimeLine_ManipulationCompleted;
            cTimeLine.PreviewTouchDown += CTimeLinePreviewTouchDown;
            cTimeLine.PreviewTouchUp += cTimeLine_PreviewTouchUp;

            cTimeLine.MouseDown += CTimeLineMouseDown;
            cTimeLine.MouseMove += CTimeLineMouseMove;
            cTimeLine.MouseUp += CTimeLineMouseUp;
            cTimeLine.MouseLeave += CTimeLineMouseLeave;
            cTimeLine.MouseWheel += CTimeLineMouseWheel;


            bFocusTime.ManipulationStarting += BFocusTimeManipulationStarting;
            bFocusTime.ManipulationDelta += BFocusTimeManipulationDelta;
            bFocusTime.ManipulationCompleted += BFocusTimeManipulationCompleted;
            bFocusTime.MouseMove += BFocusTimeMouseMove;
            bFocusTime.MouseDown += BFocusTimeMouseDown;
            bFocusTime.MouseUp += BFocusTimeMouseUp;
            bFocusTime.MouseLeave += BFocusTimeMouseLeave;
            bFocusTime.MouseWheel += BFocusTimeMouseWheel;

            #endregion

            timeUpdated = true;

            CompositionTarget.Rendering += CompositionTargetRendering;

            nowTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 2, 0) };
            nowTimer.Tick += NowTimerTick;
            nowTimer.Start();

            //player.Tick += player_Tick;

            inertiaTimelineTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 10) };
            inertiaTimelineTimer.Tick += InertiaTimelineTimerTick;

            if (Timeline != null)
            {
                Timeline.TimeChanged += TimelineTimeChanged;
                UpdateCurrentTime();
            }

            rFuture.Fill = new SolidColorBrush(AppState.AccentColor) { Opacity = 0.25 };
            //AppState.EventLists.NewEvent += EventLists_NewEvent;
            //AppState.EventLists.RemoveEvent += EventLists_NewEvent;
        }

        void MapControl_ExtentChanged(object sender, ExtentEventArgs e)
        {
            Draw();
        }


        private void EventLists_NewEvent(object sender, NewEventArgs e)
        {
            Draw();
        }



        #region mouse, touch, wheel events

        private void BFocusTimeManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            isMouseDownBm = false;
            e.Handled = true;
            Timeline.ForceTimeContentChanged();
        }

        private void CTimeLinePreviewTouchDown(object sender, TouchEventArgs e)
        {
            if (!Timeline.CanChangeTimeInterval) return;
            cTimeLine.CaptureTouch(e.TouchDevice);

        }

        private void cTimeLine_PreviewTouchUp(object sender, TouchEventArgs e)
        {
            if (!Timeline.CanChangeTimeInterval) return;
            cTimeLine.ReleaseTouchCapture(e.TouchDevice);
            if (!cTimeLine.TouchesCaptured.Any())
            {
                cTimeLine.ReleaseAllTouchCaptures();

            }

        }

        private void cTimeLine_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            if (!Timeline.CanChangeTimeInterval) return;
            cTimeLine.ReleaseAllTouchCaptures();
        }

        private void bFocusTime_TouchLeave(object sender, TouchEventArgs e)
        {
            isMouseDownBm = false;
            isMouseDown = false;
            e.Handled = true;
        }

        private void BFocusTimeMouseLeave(object sender, MouseEventArgs e)
        {
            isMouseDownBm = false;
            isMouseDown = false;
            e.Handled = true;
        }


        private DateTime bFocusMouseStart;

        private void BFocusTimeMouseUp(object sender, MouseButtonEventArgs e)
        {
            isMouseDownBm = false;
            isMouseDown = false;
            e.MouseDevice.Capture(null);
            e.Handled = true;

            if (bFocusMouseStart.AddMilliseconds(200) > AppState.TimelineManager.CurrentTime)
            {
                var menu = new MenuPopupViewModel
                {
                    RelativeElement = bFocusTime,
                    RelativePosition = new Point(-50, -15),
                    TimeOut = new TimeSpan(0, 0, 0, 5),
                    VerticalAlignment = VerticalAlignment.Bottom,
                };
                //menu.Point = _view.CreateLayer.TranslatePoint(new Point(0,0),Application.Current.MainWindow);

                menu.AddMenuItem("Go to now").Click += (s, es) =>
                {
                    Timeline.Start = AppState.TimelineManager.CurrentTime.AddDays(-1);
                    Timeline.End = AppState.TimelineManager.CurrentTime.AddDays(1);
                    Timeline.ForceTimeChanged();
                    Timeline.SetFocusTime(AppState.TimelineManager.CurrentTime);
                    Timeline.ForceTimeChanged();
                };

                AppState.Popups.Add(menu);
            }
        }

        private void BFocusTimeMouseDown(object sender, MouseButtonEventArgs e)
        {
            bFocusMouseStart = AppState.TimelineManager.CurrentTime;
            if (Timeline.CanChangeFocuseTime)
            {
                isMouseDownBm = true;
                lastMousePosBm = e.GetPosition(cTimeLine);
                e.MouseDevice.Capture((FrameworkElement)sender);

                if (Timeline.TimelinePlayer.FixFocus)
                {
                    Timeline.TimelinePlayer.FixFocus = false;
                }
            }
            e.Handled = true;
        }


        private bool focusChanged;

        private void BFocusTimeMouseMove(object sender, MouseEventArgs e)
        {
            if (Timeline.CanChangeFocuseTime && (isMouseDownBm || focusChanged))
            {
                DoMoveFocusTime(e.GetPosition(cTimeLine));
                focusChanged = false;
            }
            e.Handled = true;
        }


        private void DoMoveFocusTime(Point e)
        {
            if (!Timeline.CanChangeFocuseTime) return;
            TimeSpan dif = Timeline.End - Timeline.Start;
            double dX = -(lastMousePosBm.X - e.X);
            if (Math.Abs(dX - 0) < 0.0000001) return;
            lastMousePosBm = e;
            delta = (dif.TotalHours / cTimeLine.ActualWidth) * dX; //.TranslationX; //(e.Delta.X / 25)

            double n = ttCurentTime.X + dX;
            if (!fixHold)
            {
                if (n > 0 && n < cTimeLine.ActualWidth)
                    ttCurentTime.X = e.X;
            }
            else
            {
                ttCurentTime.X = e.X;
            }

            CalculateFocusTime();
            Timeline.ForceTimeChanged();

            if (IsFixClose())
            {
                fixHold = true;
                rFixed.Opacity = 0.5;
            }
            else
            {
                rFixed.Opacity = 0;
                fixHold = false;
            }
        }


        private void CTimeLineMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!Timeline.CanChangeTimeInterval) return;
            isMouseDown = false;
            isMouseDownBm = false;
            e.MouseDevice.Capture(null);
            //Timeline.ForceTimeContentChanged();
        }

        private void CTimeLineMouseLeave(object sender, MouseEventArgs e)
        {
            if (!Timeline.CanChangeTimeInterval) return;
            isMouseDown = false;
            isMouseDownBm = false;
            Timeline.TimelinePlayer.FixFocus = fixHold;
        }

        private void CTimeLineMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!Timeline.CanChangeTimeInterval) return;
            isMouseDown = true;
            lastMousePos = e.GetPosition(LayoutRoot);
            e.MouseDevice.Capture((FrameworkElement)sender);
        }

        private void BFocusTimeMouseWheel(object sender, MouseWheelEventArgs e)
        {
            CTimeLineMouseWheel(sender, e);
        }

        private void CTimeLineMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!Timeline.CanChangeTimeInterval) return;
            // Zoom timeline
            span = span + e.Delta;
            Point newPos = e.GetPosition(LayoutRoot);
            delta = 0;
            double newScaleD = e.Delta > 0 ? 1.5 : 0.5;
            CalculateStartEnd(delta, newScaleD, new Point(newPos.X, newPos.Y));
            //CalculateFocusTime();
            Timeline.ForceTimeChanged();
            if (fixHold)
            {
                Timeline.TimelinePlayer.FixFocus = true;
            }
            //Timeline.ForceTimeContentChanged();
        }

        private void CTimeLineMouseMove(object sender, MouseEventArgs e)
        {
            if (!Timeline.CanChangeTimeInterval) return;
            if (!isMouseDown) return;
            Point newPos = e.GetPosition(LayoutRoot);
            double xDif = lastMousePos.X - newPos.X;
            lastMousePos = newPos;
            TimeSpan dif = Timeline.End - Timeline.Start;
            delta = (dif.TotalHours / cTimeLine.ActualWidth) * -xDif;
            CalculateStartEnd(delta, 1, new Point(newPos.X, newPos.Y));
            //CalculateFocusTime();
            Timeline.ForceTimeChanged();
            if (fixHold)
            {
                Timeline.TimelinePlayer.FixFocus = true;
            }

        }

        private void InertiaTimelineTimerTick(object sender, EventArgs e)
        {
            inertiaProcessorTimeline.Process(Timestamp);
        }

        private void BFocusTimeManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (!Timeline.CanChangeFocuseTime) return;
            TimeSpan dif = Timeline.End - Timeline.Start;
            delta = (dif.TotalHours / cTimeLine.ActualWidth) * e.DeltaManipulation.Translation.X / 2.0;

            double n = ttCurentTime.X + e.DeltaManipulation.Translation.X;
            if (!fixHold)
            {
                if (n > 0 && n < cTimeLine.ActualWidth)

                    ttCurentTime.X = e.ManipulationOrigin.X;
            }
            else
            {
                ttCurentTime.X = e.ManipulationOrigin.X;
            }

            CalculateFocusTime();
            Timeline.ForceTimeChanged();

            if (IsFixClose())
            {
                fixHold = true;
                rFixed.Opacity = 1;
            }
            else
            {
                rFixed.Opacity = 0;
                fixHold = false;
            }
        }

        private void BFocusTimeManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = cTimeLine;

            isMouseDownBm = true;
        }

        private void cTimeLine_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e)
        {
            if (!Timeline.CanChangeTimeInterval) return;
            if (e.InitialVelocities.LinearVelocity.X < -1 || e.InitialVelocities.LinearVelocity.X > 1)
                e.TranslationBehavior = new InertiaTranslationBehavior
                {
                    InitialVelocity = e.InitialVelocities.LinearVelocity,
                    DesiredDeceleration = Deceleration
                };

            e.Handled = true;
        }

        private void cTimeLine_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            if (!Timeline.CanChangeTimeInterval) return;
            e.ManipulationContainer = LayoutRoot;
        }

        //domove
        private void cTimeLine_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (!Timeline.CanChangeTimeInterval) return;
            if (stopInertia && e.IsInertial)
            {
                stopInertia = false;
                e.Complete();
                return;
            }

            foreach (IManipulator tm in e.Manipulators.ToList())
                if (cTimeLine.TouchesCaptured.All(k => k.Id != tm.Id))
                {
                    Manipulation.RemoveManipulator(cTimeLine, tm);
                    //cTimeLine.ReleaseTouchCapture(tm);
                }

            span = span / e.DeltaManipulation.Scale.X; //.Delta.ScaleX;//.ScaleDelta;
            TimeSpan dif = Timeline.End - Timeline.Start;
            delta = (dif.TotalHours / cTimeLine.ActualWidth) * e.DeltaManipulation.Translation.X / 1.0;

            CalculateStartEnd(delta, e.DeltaManipulation.Scale.X,
                new Point(e.ManipulationOrigin.X, e.ManipulationOrigin.Y));

            Timeline.ForceTimeChanged();
            if (fixHold)
            {
                Timeline.TimelinePlayer.FixFocus = true;
            }

            stopInertia = false;
        }

        #endregion

        #region manipulation

        /// <summary>
        ///     Based on maninipulation calculate new start and end
        /// </summary>
        /// <param name="ddelta"></param>
        /// <param name="scale"></param>
        /// <param name="p"></param>
        private void CalculateStartEnd(double ddelta, double scale, Point p)
        {
            try
            {
                DateTime a = Timeline.Start +
                             new TimeSpan((long)((Timeline.End - Timeline.Start).Ticks * (p.X / cTimeLine.ActualWidth)));

                Timeline.Start = Timeline.Start.AddHours(-ddelta);
                Timeline.End = Timeline.End.AddHours(-ddelta);

                double hs = (Timeline.End - Timeline.Start).TotalHours;
                if (scale != 0)
                    hs = hs / scale;

                DateTime center = Timeline.Start + new TimeSpan((long)((Timeline.End - Timeline.Start).Ticks * 0.5));
                Timeline.Start = center.AddHours(hs / -2);
                Timeline.End = center.AddHours(hs / 2);

                if (scale != 1.0)
                {

                    DateTime b = Timeline.Start + new TimeSpan((long)((Timeline.End - Timeline.Start).Ticks * (p.X / cTimeLine.ActualWidth)));

                    Timeline.Start -= (b - a);
                    Timeline.End -= (b - a);
                }

                CalculateFocusTime();
            }
            catch (Exception)
            {

            }
        }


        #region timeline

        #endregion

        #region current time

        private void CalculateFocusTime()
        {
            if (HasFocus && !Timeline.TimelinePlayer.FixFocus && Timeline.CanChangeFocuseTime)
            {
                Timeline.FocusTime = Timeline.Start +
                                     new TimeSpan(
                                         (long)
                                             ((Timeline.End - Timeline.Start).Ticks *
                                              (ttCurentTime.X / cTimeLine.ActualWidth)));
            }
        }

        #endregion

        #endregion

        #region drawing

        private readonly Dictionary<string, AggregatedTimelineItem> aggregatedTimelineItems =
            new Dictionary<string, AggregatedTimelineItem>();


        private DateTime RedrawTimer = AppState.TimelineManager.CurrentTime;

        /// <summary>
        ///     Draw timeline (lines, times)
        /// </summary>
        public void Draw()
        {
            if (!AppState.TimelineManager.Visible) return;
            if (busy) return;
            if (cTimeLine == null || Timeline == null) return;
            busy = true;
            Begin();

            // calculate diff
            TimeSpan dif = Timeline.End - Timeline.Start;
            //double w = cTimeLine.ActualWidth;
            //double th = dif.TotalSeconds;



            // determine interval
            cTimeLine.Children.Clear();
            //            if (clearEvents)
            //{
            //aggregatedTimelineItems.Clear();
            //cEvents.Children.Clear();
            //}
            DateTime first = Rounding.RoundDateToMinuteInterval(Timeline.Start, Timeline.Interval, RoundingDirection.RoundUp);
            DateTime next = first;

            //var eventslist = new List<IEvent>();

            UpdateCurrentTime();
            //int rangeindex = 1;

            #region lines & times

            var ids = new List<string>();

            if (Timeline.EventsVisible )
            {
                RedrawTimer = AppState.TimelineManager.CurrentTime;

                var elist = new List<IEvent>();
                var blist = new List<Border>();
                foreach (var el in AppState.EventLists)
                {
                    elist.AddRange(AppState.EventLists.Filter(el));
                }
                elist = elist.OrderByDescending(k => k.TimeRange).ToList();
                var categories = elist.Select(k => k.Category).Distinct().ToList();
                foreach (var c in categories)
                {
                    var clist = elist.Where(k => k.Category == c);
                    //var maxparents = clist.Max(t=>clist.Count(k=>k.Date <= t.Date && k.Date.Add(k.TimeRange) >= t.Date.Add(t.TimeRange)));

                    foreach (var e in elist.Where(k => k.Category == c))
                    {
                        var y = 0;
                        var x = Timeline.GetScreenPos(e.Date);

                        var parents = elist.Where(k =>
                                    k.Category == c &&
                                    k.Date <= e.Date &&
                                    k.Date.Add(k.TimeRange) >= e.Date.Add(e.TimeRange));
                        y = parents.Count();


                        var r = Timeline.GetRow(e.Category);
                        if (r != null && r.Visible)
                        {
                            Border b = new Border()
                            {
                                BorderBrush = new SolidColorBrush(e.Color),
                                Background = Brushes.Transparent,
                                BorderThickness = new Thickness(2),
                                CornerRadius = new CornerRadius(4),
                                Width = 8,
                                Height = 8,
                                Tag = e.Id
                            };
                            if (e.TimeRange != new TimeSpan())
                            {
                                var x2 = Timeline.GetScreenPos(e.Date.Add(e.TimeRange));
                                if (x2 - x > 8)
                                    b.Width += (x2 - x) - 8;
                            }
                            b.MouseDown += (f, p) =>
                            {
                                e.TriggerClicked(this, null);
                            };
                            b.MouseEnter += (f, p) =>
                            {
                                var item = new TimeItemViewModel() { Item = e, CustomItem = true };
                                var bt = ViewLocator.LocateForModel(item, null, null) as FrameworkElement;
                                ViewModelBinder.Bind(item, bt, null);
                                if (e.TimeRange != new TimeSpan())
                                    bt.RenderTransform = new TranslateTransform(x + p.GetPosition(b).X, -55);
                                else
                                    bt.RenderTransform = new TranslateTransform(x - 2, -55);
                                ActiveEvent.Children.Add(bt);
                            };
                            b.MouseLeave += (f, p) =>
                            {
                                ActiveEvent.Children.Clear();
                            };
                            if (categories.Count() > 1)
                                b.RenderTransform = new TranslateTransform(x - 4, r.ActualOrder * 7 + 5);
                            else
                                b.RenderTransform = new TranslateTransform(x - 4, 7 * y + r.ActualOrder * 7 + 5);
                            blist.Add(b);
                        }
                    }
                }

                cEvents.Children.Clear();
                foreach (var b in blist)
                {
                    cEvents.Children.Add(b);
                }
                //var addlist = blist.Where(b => cEvents.Children.OfType<Border>().All(k => k.Tag != b.Tag)).ToList();
                //var remlist = cEvents.Children.OfType<Border>().Where(b => blist.All(k => k.Tag != b.Tag)).ToList();
                //foreach ( var b in addlist)
                //    cEvents.Children.Add(b);
                //foreach ( var b in remlist)
                //    cEvents.Children.Add(b);
            }





            while (next < Timeline.End)
            {
                string id = next.Ticks + "-" + next.AddSeconds(Timeline.Interval).Ticks;
                ids.Add(id);
                var l = new Line
                {
                    X1 = Timeline.GetScreenPos(next),// (w/th)*((next - Timeline.Start).TotalSeconds),
                    Y1 = 0,
                    Y2 = gContent.ActualHeight - 1,
                    StrokeThickness = DividerWidth,
                    Stroke = Timeline.DividerBrush
                };
                l.X2 = l.X1;
                cTimeLine.Children.Add(l);

                /*
                if (Timeline.EventsVisible)
                {
                   

                    //Events
                    DateTime next2 = next.AddSeconds(Timeline.Interval);
                    double x2 = Timeline.GetScreenPos(next); // (w/th)*((next2 - Timeline.Start).TotalSeconds);

                    AggregatedTimelineItem at;

                    if (aggregatedTimelineItems.ContainsKey(id))
                    {
                        at = aggregatedTimelineItems[id];
                        double aWidth = x2 - l.X1;
                        at.Width = aWidth;
                        at.RenderTransform = new TranslateTransform(l.X1, 0);
                    }
                    else
                    {
                        at = new AggregatedTimelineItem
                        {
                            DividerWidth = DividerWidth,
                            Height = 30,
                            RenderTransform = new TranslateTransform(l.X1, 0)
                        };
                        at.Childs.Children.Clear();

                        double aWidth = x2 - l.X1;
                        at.Width = aWidth;
                        cEvents.Children.Add(at);

                        //var aggrevents = (from tli in timelineItems let ip = FindPos(tli.ItemDateTime) where ip >= l.X1 && ip < x2 select tli).ToList();
                        List<IEvent> aggrevents = (from tli in AppState.EventLists.FilteredList
                            let ip = FindPos(tli.Date)
                            let ip2 = FindPos(tli.Date.Add(tli.TimeRange))
                            where
                                eventslist.All(g => g.Id != tli.Id) &&
                                ((ip >= l.X1 && ip < x2) || (ip < Math.Min(l.X1, -1) && ip2 >= l.X1 && ip2 < x2))
                            select tli).ToList();
                        eventslist.AddRange(aggrevents);

                        foreach (IEvent e in aggrevents)
                        {
                            //var margin = FindPos(e.ItemDateTime) - l.X1 - at.Width/2.0;
                            //var marginwidth = FindPos(e.ItemDateTime.Add(e.ItemRange)) - l.X1 - at.Width / 2.0;
                            double margin = FindPos(e.Date) - l.X1; // -at.Width / 2.0;
                            double marginwidth = FindPos(e.Date.Add(e.TimeRange)) - l.X1; // -at.Width / 2.0;
                            var bord = new Border
                            {
                                CornerRadius = new CornerRadius(10.0),
                                BorderBrush = new SolidColorBrush(Colors.Black),
                                Background = new SolidColorBrush(Colors.White),
                                BorderThickness = new Thickness(2),
                                VerticalAlignment = VerticalAlignment.Top,
                                Margin = new Thickness(-7, 0, 0, 0),
                                Width = 10,
                                Height = 10,
                                Tag = e.Name,
                                RenderTransform = new TranslateTransform(margin, 12)
                            };
                            if (marginwidth != margin)
                            {
                                double width = marginwidth - margin + 10.0;
                                bord.Width = width;
                                bord.Height = 5;
                                var tg = new TransformGroup();
                                tg.Children.Add(new TranslateTransform(FindPos(e.Date), 5 + rangeindex*7));
                                bord.CornerRadius = new CornerRadius(0.0);
                                if (margin + width > aWidth)
                                {
                                    //bord.Width = 100;
                                    //tg.Children.Add(new ScaleTransform(width/100, 1));
                                }
                                bord.RenderTransform = tg;
                                rangeindex++;
                            }

                            bord.MouseEnter += delegate(Object sender, MouseEventArgs te)
                            {
                                var tg = new TransformGroup();
                                //var width = marginwidth - margin + 10.0;
                                if (bord.Width == 10)
                                    tg.Children.Add(new TranslateTransform(margin - 3, -20));
                                else
                                {
                                    Point p = te.GetPosition(cEvents);
                                    tg.Children.Add(new TranslateTransform(p.X - l.X1, -20));
                                }

                                //if (margin + width > aWidth)
                                //{
                                //    var p = te.GetPosition(at);
                                //    tg.Children.Add(new TranslateTransform(p.X,0));
                                //}
                                at.bEvent.RenderTransform = tg;
                                at.ShowEvent(e);
                                //if (e.EventPoint.Latitude != 0 && e.EventPoint.Longitude != 0)
                                if (e.Latitude != 0 && e.Longitude != 0)
                                    at.bZoom.Visibility = Visibility.Visible;
                                else
                                {
                                    at.bZoom.Visibility = Visibility.Collapsed;
                                }
                                te.Handled = true;
                            };
                            bord.MouseLeave += delegate(Object sender, MouseEventArgs te)
                            {
                                at.RemoveLater();
                                te.Handled = true;
                            };
                            bord.TouchEnter += delegate(Object sender, TouchEventArgs te)
                            {
                                at.bEvent.RenderTransform = new TranslateTransform(margin - 3, -20);
                                at.ShowEvent(e);
                                te.Handled = true;
                            };
                            bord.TouchLeave += delegate(Object sender, TouchEventArgs te)
                            {
                                at.RemoveLater();
                                te.Handled = true;
                            };
                            if (bord.Width > 10)
                            {
                                cEvents.Children.Add(bord);
                                Panel.SetZIndex(bord, 99999);
                            }
                            else
                                at.Childs.Children.Add(bord);
                        }
                        aggregatedTimelineItems.Add(id, at);
                    }
                }
                 */


                if (Timeline.Interval <= 60)
                {
                    var tbTime = new TextBlock();
                    tbTime.SetValue(Canvas.LeftProperty, l.X1 + 5);
                    tbTime.SetValue(Canvas.BottomProperty, 22.0);
                    tbTime.Foreground = Timeline.Foreground;
                    tbTime.FontSize = 18.0;
                    tbTime.IsHitTestVisible = false;
                    tbTime.Text = next.ToString("HH:mm:ss");
                    cTimeLine.Children.Add(tbTime);

                    var tbDate = new TextBlock();
                    tbDate.SetValue(Canvas.LeftProperty, l.X1 + 5);
                    tbDate.SetValue(Canvas.BottomProperty, 10.0);
                    tbDate.FontSize = 10.0;
                    tbDate.Foreground = Timeline.Foreground;
                    tbDate.Text = next.ToString("dd-MM");
                    tbDate.Opacity = 0.75;
                    tbDate.IsHitTestVisible = false;
                    cTimeLine.Children.Add(tbDate);
                }
                else if (Timeline.Interval < 86400)
                {
                    var tbTime = new TextBlock();
                    tbTime.SetValue(Canvas.LeftProperty, l.X1 + 5);
                    tbTime.SetValue(Canvas.BottomProperty, 22.0);
                    tbTime.Foreground = Timeline.Foreground;
                    tbTime.FontSize = 18.0;
                    tbTime.IsHitTestVisible = false;
                    tbTime.Text = next.ToShortTimeString(); // next.Hour + ":" + next.Minute;
                    cTimeLine.Children.Add(tbTime);

                    var tbDate = new TextBlock();
                    tbDate.SetValue(Canvas.LeftProperty, l.X1 + 5);
                    tbDate.SetValue(Canvas.BottomProperty, 10.0);
                    tbDate.FontSize = 10.0;
                    tbDate.IsHitTestVisible = false;
                    tbDate.Foreground = Timeline.Foreground;
                    tbDate.Text = next.ToString("dd-MM");
                    tbDate.Opacity = 0.75;
                    cTimeLine.Children.Add(tbDate);
                }
                else if (Timeline.Interval < 31536000)
                {
                    var tbDate = new TextBlock();
                    tbDate.SetValue(Canvas.LeftProperty, l.X1 + 5);
                    tbDate.SetValue(Canvas.BottomProperty, 15.0);
                    tbDate.FontSize = 18.0;
                    tbDate.IsHitTestVisible = false;
                    tbDate.Foreground = Timeline.Foreground;
                    tbDate.Text = next.ToString("dd-MM-yyyy");
                    cTimeLine.Children.Add(tbDate);
                }
                else
                {
                    var tbDate = new TextBlock();
                    tbDate.SetValue(Canvas.LeftProperty, l.X1 + 5);
                    tbDate.SetValue(Canvas.BottomProperty, 15.0);
                    tbDate.FontSize = 18.0;
                    tbDate.IsHitTestVisible = false;
                    tbDate.Foreground = Timeline.Foreground;
                    tbDate.Text = next.ToString("yyyy");
                    cTimeLine.Children.Add(tbDate);
                }
                next = next.AddSeconds(Timeline.Interval);
            }

            //List<string> tbr = aggregatedTimelineItems.Keys.Where(k => !ids.Contains(k)).ToList();
            //foreach (string t in tbr)
            //{
            //    cEvents.Children.Remove(aggregatedTimelineItems[t]);
            //    aggregatedTimelineItems.Remove(t);
            //}

            #endregion

            End();
            busy = false;
        }


        private double FindPos(DateTime dt)
        {
            TimeSpan dif = Timeline.End - Timeline.Start;
            double w = Application.Current.MainWindow.ActualWidth;
            double th = dif.TotalSeconds;
            double totalsecs = (dt - Timeline.Start).TotalSeconds;
            return (w / th) * totalsecs;
        }

        /// <summary>
        ///     Update current time (small red line)
        /// </summary>
        private void UpdateCurrentTime()
        {
            try
            {
                //Timeline.CurrentTime = DateTime.Now;
                if (Timeline.TimelinePlayer.FixFocus) Timeline.FocusTime = Timeline.CurrentTime;
                AppStateSettings.Instance.ViewDef.MapControl.TimeExtent = new TimeExtent(Timeline.Start, Timeline.End);
                //Timeline.CurrentTime = DateTime.Now;
                if (cTimeLine.Children.Contains(LNow)) cTimeLine.Children.Remove(LNow);

                if (Timeline.Interval <= 10)
                {
                    tbCurrentDate.Text = Timeline.FocusTime.ToShortDateString();
                    tbCurrentTime.Text = Timeline.FocusTime.ToString("mm:ss.fff");
                }
                else if (Timeline.Interval <= 160)
                {
                    tbCurrentDate.Text = Timeline.FocusTime.ToShortDateString();
                    tbCurrentTime.Text = Timeline.FocusTime.ToString("HH:mm:ss");
                }
                else if (Timeline.Interval < 186400)
                {
                    tbCurrentDate.Text = Timeline.FocusTime.ToShortDateString();
                    tbCurrentTime.Text = Timeline.FocusTime.ToString("HH:mm");
                }
                else if (Timeline.Interval < 11536000)
                {
                    tbCurrentDate.Text = Timeline.FocusTime.ToShortDateString();
                    tbCurrentTime.Text = Timeline.FocusTime.Hour + "h";
                }
                else
                {
                    tbCurrentTime.Text = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Timeline.FocusTime.Month);
                    tbCurrentDate.Text = Timeline.FocusTime.ToString("yyyy");
                    //""; // Timeline.FocusTime.ToShortTimeString();
                }

                bFocusTime.RenderTransform = new TranslateTransform(FindPos(Timeline.FocusTime) - 12.5, 0);

                if (Timeline.CurrentTime < Timeline.End)
                {
                    var x = FindPos(Timeline.CurrentTime);

                    rFuture.Margin = new Thickness(Math.Max(x, 0), 0, 0, 0);
                    if (LayoutRoot.ActualWidth > x) rFuture.Width = LayoutRoot.ActualWidth - x;

                    if (Timeline.CurrentTime <= Timeline.Start) return;
                    LNow = new Line { X1 = x };
                    LNow.X2 = LNow.X1;
                    LNow.Y1 = 0;
                    LNow.Y2 = cTimeLine.ActualHeight;
                    LNow.StrokeThickness = 2.0;
                    LNow.Stroke = AppState.AccentBrush; // Timeline.CurrentTimeBrush;
                    cTimeLine.Children.Add(LNow);
                }
                else
                {
                    rFuture.Width = 0;
                }
            }
            catch (Exception)
            {
                // Console.WriteLine(@"Error updating Timeline");
            }
        }

        private void End()
        {
            cTimeLine.EndInit();
        }

        private void Begin()
        {
            cTimeLine.BeginInit();
            //Clear();
        }

        private void bFocusTime_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        #endregion

        //#region player

        //private readonly DispatcherTimer player = new DispatcherTimer();

        //private void rewind(object sender, MouseButtonEventArgs e)
        //{
        //    TimeSpan timespan = Timeline.PlayEnd - Timeline.PlayStart;
        //    player.Stop();
        //    Timeline.FocusTime = Timeline.PlayStart;
        //    Timeline.Start = Timeline.PlayStart.AddMilliseconds(-timespan.TotalMilliseconds/10.0);
        //    Timeline.End = Timeline.PlayEnd.AddMilliseconds(timespan.TotalMilliseconds/10.0);
        //}

        //private void play(object sender, MouseButtonEventArgs e)
        //{
        //    player.Interval = Timeline.PlaySpeed;
        //    player.Start();
        //}

        //private void stop(object sender, MouseButtonEventArgs e)
        //{
        //    player.Stop();
        //}

        //private void pause(object sender, MouseButtonEventArgs e)
        //{
        //    player.Stop();
        //}

        //private void player_Tick(object sender, EventArgs e)
        //{
        //    if (Timeline.FocusTime.AddMilliseconds(Timeline.PlayStepSize.TotalMilliseconds*playfactor) > Timeline.PlayEnd)
        //        Timeline.FocusTime = Timeline.PlayStart;
        //    else
        //        Timeline.FocusTime = Timeline.FocusTime.AddMilliseconds(Timeline.PlayStepSize.TotalMilliseconds*playfactor);
        //    Draw();
        //    Timeline.ForceTimeChanged();
        //}

        //private double playfactor = 1;

        //private void PlaySizeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        //{
        //    playfactor = e.NewValue;
        //}

        //#endregion
    }
}