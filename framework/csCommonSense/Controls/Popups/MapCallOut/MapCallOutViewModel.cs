using BaseWPFHelpers;
using Caliburn.Micro;
using csShared.FloatingElements;
using csShared.Interfaces;
using csShared.Utils;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using Microsoft.Surface.Presentation.Controls;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using DataServer;
using System.Windows.Controls;


namespace csShared.Controls.Popups.MapCallOut
{
    [Export(typeof(IPopupScreen))]
    public class MapCallOutViewModel : Screen, IPopupScreen
    {
        private bool autoClose = true;
        private ObservableCollection<CallOutAction> actions = new ObservableCollection<CallOutAction>();
        private Brush backgroundBrush = (Brush)new BrushConverter().ConvertFromString("#CC000000");
        private bool canClose = true;
        private MapCallOutView mcov;

        public event EventHandler Tapped;

        private MapPoint point;
        private Thickness pos;
        private Timer toTimer;
        private Screen viewModel;
        private double width;
        private double height = 100;
        private Graphic graphic;

        private bool pinned;

        public bool Pinned
        {
            get { return pinned; }
            set { pinned = value; NotifyOfPropertyChange(() => Pinned); }
        }

        private CallOutOrientation orientation = CallOutOrientation.Right;

        public CallOutOrientation Orientation
        {
            get { return orientation; }
            set { orientation = value; NotifyOfPropertyChange(() => Orientation); }
        }

        public void StopTimer()
        {
            if (toTimer!=null) toTimer.Stop();
        }

        public void StartTimer()
        {
            if (toTimer == null) return;
            toTimer.Start();
        }

        public Graphic Graphic
        {
            get { return graphic; }
            set
            {
                graphic = value;
                NotifyOfPropertyChange(() => Graphic);
            }
        }

        public MapPoint Point
        {
            get { return point; }
            set
            {
                point = value;
                NotifyOfPropertyChange(() => Point);
            }
        }

        public ObservableCollection<CallOutAction> Actions
        {
            get { return actions; }
            set
            {
                actions = value;
                NotifyOfPropertyChange(() => Actions);
            }
        }

        public TimeSpan? TimeOut { get; set; }

        public Thickness Pos
        {
            get { return pos; }
            set
            {
                pos = value;
                NotifyOfPropertyChange(() => Pos);
            }
        }

        private bool showArrow;

        public bool ShowArrow
        {
            get { return showArrow; }
            set { showArrow = value; NotifyOfPropertyChange(() => ShowArrow); }
        }


        public double Width
        {
            get { return width; }
            set
            {
                width = value;
                NotifyOfPropertyChange(() => Width);
            }
        }


        public double Height
        {
            get { return height; }
            set
            {
                height = value;
                NotifyOfPropertyChange(() => Height);
                //                UpdateCallout();
            }
        }

        public Screen ViewModel
        {
            get { return viewModel; }
            set
            {
                viewModel = value;
                NotifyOfPropertyChange(() => ViewModel);
                UpdateCallout();
            }
        }

        private Brush foregroundBrush;

        public Brush ForegroundBrush
        {
            get { return foregroundBrush; }
            set
            {
                foregroundBrush = value;
                NotifyOfPropertyChange(() => ForegroundBrush);
            }
        }

        public new bool CanClose // FIXME TODO "new" keyword missing?
        {
            get { return canClose; }
            set
            {
                canClose = value;
                NotifyOfPropertyChange(() => CanClose);
            }
        }

        public Brush BackgroundBrush
        {
            get { return backgroundBrush; }
            set
            {
                backgroundBrush = value;
                NotifyOfPropertyChange(() => BackgroundBrush);
            }
        }

        private double maxWidth;

        public double MaxWidth
        {
            get { return double.IsNaN(maxWidth) ? 250 : maxWidth; }
            set { maxWidth = value; NotifyOfPropertyChange(() => MaxWidth); }
        }

        private double maxHeight;

        public double MaxHeight
        {
            get { return maxHeight; }
            set { maxHeight = value; NotifyOfPropertyChange(() => MaxHeight); }
        }


        private Thickness gridMargin;

        public Thickness GridMargin
        {
            get { return gridMargin; }
            set { gridMargin = value; NotifyOfPropertyChange(() => GridMargin); }
        }

        private Thickness pathMargin;

        public Thickness PathMargin
        {
            get { return pathMargin; }
            set { pathMargin = value; NotifyOfPropertyChange(() => PathMargin); }
        }

        private bool canPin;
        public bool CanPin
        {
            get { return canPin; }
            set { canPin = value; NotifyOfPropertyChange(() => CanPin); }
        }

        private bool pinEnabled;
        public bool PinEnabled
        {
            get { return pinEnabled; }
            set { pinEnabled = value; NotifyOfPropertyChange(() => PinEnabled); }
        }

        public bool AutoClose
        {
            get { return autoClose; }
            set {
                autoClose = value;
                if (!autoClose) StopTimer();
            }
        }

        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public event EventHandler Closed;

        public void Close()
        {
            Close(null);
        }

        /// <summary>
        /// When binding a property to a textbox/combox the property is not updated
        /// when a menu item is selected (because this doesn't cause a focus change)
        /// This method writes the value in the gui to the property from the active element!
        /// </summary>
        public static void DoSimulateFocusChange()
        {
            TextBox textBox = Keyboard.FocusedElement as TextBox;

            if (textBox != null)
            {
                BindingExpression be = textBox.GetBindingExpression(TextBox.TextProperty);
                if (be != null && !textBox.IsReadOnly && textBox.IsEnabled)
                {
                    be.UpdateSource();
                }
                return;
            }
            ComboBox lComboBox = Keyboard.FocusedElement as ComboBox;

            if (lComboBox != null)
            {
                BindingExpression be = lComboBox.GetBindingExpression(ComboBox.TextProperty);
                if (be != null && !lComboBox.IsReadOnly && lComboBox.IsEnabled)
                {
                    be.UpdateSource();
                }
                return;
            }

           
        }

        public void Close(EventArgs e)
        {

            //if (e is RoutedEventArgs)
            //{
            //    var me = e as RoutedEventArgs;
            //    me.Handled = true;
            //}
            //if (e is TouchEventArgs)
            //{
            //    var te = e as TouchEventArgs;
            //    te.Handled = true;
            //}
            try
            {
                DoSimulateFocusChange();
                if (ViewModel != null)
                {
                    var viewType = ViewModel.GetType();
                    var closeMethod = viewType.GetMethod("Close");
                    if (closeMethod != null) closeMethod.Invoke(ViewModel, null);
                }
                if (Pinned)
                {
                    MaxWidth = double.NaN;
                    var svi = (ScatterViewItem)Helpers.FindElementOfTypeUp(mcov, typeof(ScatterViewItem));
                    if (svi == null) return;
                    var fe = (FloatingElement)svi.DataContext;
                    fe.Close();
                }
                else
                {
                    MaxWidth = Width;
                    AppStateSettings.Instance.ViewDef.MapControl.ExtentChanging -= MapControlExtentChanging;
                    AppStateSettings.Instance.ViewDef.MapControl.ExtentChanged -= MapControlExtentChanged;
                    if (Graphic != null) Graphic.PropertyChanged -= GraphicPropertyChanged;
                    if (Closed != null) Closed(this, null);
                    if (AppState.Popups.Contains(this)) AppState.Popups.Remove(this);
                }
            }
            catch (Exception exp)
            {
                Logger.Log("Map Callout", "Error removing callout", exp.Message, Logger.Level.Error);
            }
        }

        public void Clicked()
        {
            var handler = Tapped;
            if (handler != null) handler(this, null);
        }

        public void Clicked2(TouchEventArgs e)
        {
            e.Handled = true;
        }

        public void Drag(object sender, CallOutAction context, EventArgs e)
        {
            if (context.IsDraggable)
            {
                context.TriggerDragStart(sender, e);
            }
        }

        public void Pin(EventArgs e = null)
        {
            //if (e is RoutedEventArgs)
            //{
            //    var me     = e as RoutedEventArgs;
            //    me.Handled = true;
            //}
            var fe = FloatingHelpers.CreateFloatingElement(Title, DockingStyles.None, this, "", 10);
            fe.CanScale = true;
            //fe.Style       = Application.Current.FindResource("SimpleContainer") as Style;
            fe.Style = Application.Current.FindResource("SimpleContainer") as Style;
            fe.StartPosition = new Point(Pos.Left + Width / 2, Pos.Top + ((Height + 75) / 2));
            if (mcov != null && mcov.ViewModel != null)
                fe.StartSize = new Size(mcov.ViewModel.ActualWidth + 50, mcov.ViewModel.ActualHeight + 125);
            else
                fe.StartSize = new Size(Width + 20, Height + 75);
            fe.Background = BackgroundBrush;
            fe.Foreground = ForegroundBrush;
            fe.Title = Title;
            AppState.FloatingItems.AddFloatingElement(fe);
            Close();
            Pinned = true;
            AutoClose = false;
            StopTimer();
            UpdateCallout();
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            mcov = (MapCallOutView)view;
            AppStateSettings.Instance.ViewDef.MapControl.ExtentChanging += MapControlExtentChanging;
            AppStateSettings.Instance.ViewDef.MapControl.ExtentChanged += MapControlExtentChanged;
            mcov.MouseEnter += mcov_MouseEnter;
            mcov.MouseLeave += mcov_MouseLeave;
            EnableZoom(ForegroundBrush);
            CreateAndStartTimer();

            if (Graphic != null)
                Graphic.PropertyChanged += GraphicPropertyChanged;
            UpdateCallout();
        }

        void mcov_MouseLeave(object sender, MouseEventArgs e)
        {
            if (Pinned || !AutoClose) return;
            CreateAndStartTimer();
        }

        void mcov_MouseEnter(object sender, MouseEventArgs e) {
            if (toTimer!=null) toTimer.Stop();
        }

        private void CreateAndStartTimer()
        {
            if (TimeOut.HasValue)
            {
                // If you have a really large value, do not set a timer.
                if (TimeOut.Value.TotalSeconds > 1000) return;
            }
            else 
            {
                TimeOut = new TimeSpan(0, 0, 0, 0, 1500);
            }
            toTimer = new Timer { Interval = TimeOut.Value.TotalMilliseconds };
            toTimer.Elapsed += ToTimerTick;
            toTimer.Start();
        }

        private HorizontalAlignment contentAlignment;

        public HorizontalAlignment ContentAlignment
        {
            get { return contentAlignment; }
            set { contentAlignment = value; NotifyOfPropertyChange(() => ContentAlignment); }
        }

        private HorizontalAlignment gridAlignment;
        public HorizontalAlignment GridAlignment
        {
            get { return gridAlignment; }
            set { gridAlignment = value; NotifyOfPropertyChange(() => GridAlignment); }
        }

        private VerticalAlignment contentVerticalAlignment;
        public VerticalAlignment ContentVerticalAlignment
        {
            get { return contentVerticalAlignment; }
            set { contentVerticalAlignment = value; NotifyOfPropertyChange(() => ContentVerticalAlignment); }
        }

        private double pathOrientation;
        public double PathOrientation
        {
            get { return pathOrientation; }
            set { pathOrientation = value; NotifyOfPropertyChange(() => PathOrientation); }
        }

        private void GraphicPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var geometry = Graphic.Geometry as MapPoint;
            if (geometry == null) return;
            Point = geometry;
            UpdateCallout();
        }

        public void TapAction(CallOutAction action, EventArgs e)
        {
            if (action != null) action.TriggerClicked(e);
        }

        private HorizontalAlignment pathHorizontalAlignment;

        public HorizontalAlignment PathHorizontalAlignment
        {
            get { return pathHorizontalAlignment; }
            set { pathHorizontalAlignment = value; NotifyOfPropertyChange(() => PathHorizontalAlignment); }
        }

        private VerticalAlignment pathVerticalAlignment = VerticalAlignment.Bottom;

        public VerticalAlignment PathVerticalAlignment
        {
            get { return pathVerticalAlignment; }
            set { pathVerticalAlignment = value; NotifyOfPropertyChange(() => PathVerticalAlignment); }
        }


        private void ToTimerTick(object sender, EventArgs e)
        {
            toTimer.Stop();

            if (!Pinned && AutoClose && CanPin)
                Close();
        }

        public void EnableZoom(Brush iconBrush)
        {
            var coa = new CallOutAction
            {
                IconBrush = ForegroundBrush,
                Title = "Zoom",
                Path = "F1M1937.77,2316.51L1924.92,2300.18C1929.1,2294.35 1929.14,2286.25 1924.48,2280.33 1918.84,2273.15 1908.45,2271.92 1901.27,2277.56 1894.1,2283.2 1892.86,2293.59 1898.5,2300.77 1903.23,2306.77 1911.26,2308.6 1917.96,2305.74L1930.77,2322.02 1937.77,2316.51z M1903.81,2296.59C1900.48,2292.35 1901.21,2286.21 1905.45,2282.87 1909.69,2279.53 1915.84,2280.26 1919.17,2284.51 1922.51,2288.75 1921.78,2294.89 1917.54,2298.23 1913.29,2301.57 1907.15,2300.84 1903.81,2296.59z"
            };
            coa.Clicked += (e, f) =>
            {
                AppStateSettings.Instance.ViewDef.MapControl.ZoomDuration = new TimeSpan(0, 0, 0, 1);
                AppStateSettings.Instance.ViewDef.MapControl.ZoomToResolution(1, point);
            };
            Actions.Add(coa);
        }

        private void MapControlExtentChanged(object sender, ExtentEventArgs e)
        {
            UpdateCallout();
        }

        private void MapControlExtentChanging(object sender, ExtentEventArgs e)
        {
            UpdateCallout();
        }

        public void UpdateCallout()
        {
            if (Pinned)
            {
                MaxWidth = double.NaN;
                Pos = new Thickness(0, 0, 0, 0);
                //ContentVerticalAlignment = VerticalAlignment.Stretch;
                ShowArrow = false;
                CanClose = true;
                CanPin = false;
                ContentAlignment = HorizontalAlignment.Stretch;
                GridAlignment = HorizontalAlignment.Stretch;
            }
            else
            {
                MaxWidth = Width;
                ContentAlignment = HorizontalAlignment.Left;
                ContentVerticalAlignment = VerticalAlignment.Top;
                ShowArrow = true;

                CanClose = true;
                if (PinEnabled)
                    CanPin = true;
                var p = AppStateSettings.Instance.ViewDef.MapControl.MapToScreen(Point);
                switch (orientation)
                {
                    case CallOutOrientation.Top:
                        Pos = new Thickness(p.X - Width / 2, 0, 0, AppStateSettings.Instance.ViewDef.MapControl.ActualHeight - p.Y + 20);
                        GridMargin = new Thickness(0, 0, 0, 20);
                        PathMargin = new Thickness(0, 0, 0, 0);
                        PathOrientation = 0.0;
                        PathHorizontalAlignment = HorizontalAlignment.Center;
                        GridAlignment = HorizontalAlignment.Center;
                        break;
                    case CallOutOrientation.Right:
                        PathVerticalAlignment = VerticalAlignment.Top;
                        PathOrientation = 90.0;
                        GridMargin = new Thickness(10, 0, 0, 0);
                        Pos = new Thickness(p.X + 20, Math.Max(p.Y - 100, 0), 0, Math.Min((p.Y), 0));
                        PathMargin = new Thickness(0, p.Y < 80 ? Math.Max(p.Y, -20) : 80, 0, 0);
                        PathHorizontalAlignment = HorizontalAlignment.Left;
                        GridAlignment = HorizontalAlignment.Left;

                        if (p.Y < -20 && mcov != null && mcov.Visibility == Visibility.Visible)
                            mcov.Visibility = Visibility.Collapsed;
                        else if (p.Y > -20 && mcov != null && mcov.Visibility == Visibility.Collapsed)
                            mcov.Visibility = Visibility.Visible;
                        break;
                    case CallOutOrientation.RightSideMenu:
                        PathVerticalAlignment = VerticalAlignment.Top;
                        PathOrientation = 90.0;
                        GridMargin = new Thickness(0, 0, 0, 0);
                        Pos = new Thickness(mcov.ActualWidth - Width, 0, 0, 0);
                        mcov.ContentGrid.Height = mcov.Height = (mcov.Parent as System.Windows.Controls.ContentControl).ActualHeight; // TODO Must be improved
                        mcov.ContentGrid.Width = Width;
                        PathMargin = new Thickness(0, 0, 0, 0);
                        PathHorizontalAlignment = HorizontalAlignment.Left;
                        GridAlignment = HorizontalAlignment.Left;

                        if (p.Y < -20 && mcov != null && mcov.Visibility == Visibility.Visible)
                            mcov.Visibility = Visibility.Collapsed;
                        else if (p.Y > -20 && mcov != null && mcov.Visibility == Visibility.Collapsed)
                            mcov.Visibility = Visibility.Visible;
                        break;
                }
            }
        }

        private string title;

        public string Title
        {
            get { return title; }
            set { title = value; NotifyOfPropertyChange(() => Title); }
        }

        public void StartEditing()
        {
            Actions.Remove(Actions.FirstOrDefault(action => string.Equals(action.Title, "Edit", StringComparison.InvariantCultureIgnoreCase)));
            //if (!Pinned) Pin();
            OnEditing();
        }

        public delegate void EditingEventHandler(object sender, EventArgs e);

        public event EditingEventHandler Editing;

        protected virtual void OnEditing()
        {
            var handler = Editing;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public void ResetTimer()
        {
            if (toTimer == null) return;
            toTimer.Stop();
            toTimer.Start();
        }
    }


}