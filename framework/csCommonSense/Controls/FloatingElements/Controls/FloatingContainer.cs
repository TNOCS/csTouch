using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using BaseWPFHelpers;
using Caliburn.Micro;
using csCommon;
using csCommon.Controls;
using csImb;
using csShared.Controls.Popups.MenuPopup;
using csShared.Documents;
using csShared.FloatingElements.Classes;
using csShared.Utils;
using Microsoft.Surface.Presentation.Controls;
using Microsoft.Surface.Presentation.Generic;
using Microsoft.Surface.Presentation.Input;

namespace csShared.FloatingElements
{
    public class FloatingContainer : Control
    {
        public static readonly RoutedEvent CloseEvent =
            EventManager.RegisterRoutedEvent("Close", RoutingStrategy.Bubble,
                typeof (RoutedEventHandler), typeof (FloatingContainer));

        public static readonly RoutedEvent ResetEvent =
            EventManager.RegisterRoutedEvent("Reset", RoutingStrategy.Bubble,
                typeof (RoutedEventHandler), typeof (FloatingContainer));


        // Using a DependencyProperty as the backing store for Undocked. This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UndockedProperty =
            DependencyProperty.Register("Undocked", typeof (bool), typeof (FloatingContainer),
                new UIPropertyMetadata(false));

        private readonly bool _reverse; // FIXME TODO _reverse is never assigned.
        private DispatcherTimer _autoCloseTimer;
        private ContentControl _cc;

        public FrameworkElement _cpView;

        public FloatingElement _fe;
        private IFloating _floatingViewModel;
        private DateTime _lastBlobEvent; // FIXME TODO _lastBlobEvent is assigned but not used.
        private FloatingElement _lastDragElement;
        private Line _partAssociation;
        private Grid _partContent;
        private FrameworkElement _partPreview;
        private SurfaceButton _partStream;
        private double _previousAngle;
        private Point _previousCenter;
        private double _previousHeight;
        private Style _previousStyle;
        private double _previousWidth;

        private Border _resize;
        private FrameworkElement _sv;
        private ScatterViewItem _svi;
        private DateTime _touchDown;
        private List<EndPoint> endPoints = new List<EndPoint>();
        private List<FloatingElement> hitResultsList = new List<FloatingElement>();

        static FloatingContainer() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof (FloatingContainer),
                new FrameworkPropertyMetadata(typeof (FloatingContainer)));
        }

        public AppStateSettings AppState {
            get { return AppStateSettings.Instance; }
        }

        public bool Undocked {
            get { return (bool) GetValue(UndockedProperty); }
            set { SetValue(UndockedProperty, value); }
        }

        public event RoutedEventHandler Close {
            add { AddHandler(CloseEvent, value); }
            remove { RemoveHandler(CloseEvent, value); }
        }

        protected override void OnInitialized(EventArgs e) {
            base.OnInitialized(e);
            InitScatterViewEvents();
            if (_fe.AllowStream && AppState.Imb != null) {
                AppState.Imb.ClientAdded += ClientsUpdated;
                AppState.Imb.ClientRemoved += Imb_ClientRemoved;
                AppState.Imb.ClientChanged += ClientsUpdated;
            }
            UpdateStreamVisibility();
            //if (_fe.AssociatedPoint != null)
            //{
            //  var l = new Line {X1 = 0, X2 = 0};
            //}
            if (_fe.AutoClose == FloatingElement.AutoCloseStyle.None) return;
            _autoCloseTimer = new DispatcherTimer {Interval = new TimeSpan(0, 0, _fe.AutoCloseTimeout)};
            _autoCloseTimer.Tick += (s, f) => {
                switch (_fe.AutoClose) {
                    case FloatingElement.AutoCloseStyle.NoInteraction:
                        if (_touchDown.AddSeconds(_fe.AutoCloseTimeout) < DateTime.Now) {
                            _autoCloseTimer.Stop();
                            CloseButtonClick(this, null);
                        }
                        break;
                    case FloatingElement.AutoCloseStyle.Always:
                        _autoCloseTimer.Stop();
                        CloseButtonClick(this, null);
                        break;
                }
            };
            _autoCloseTimer.Start();

            _fe.PropertyChanged += _fe_PropertyChanged;
        }

        private void _fe_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "ModelInstanceBack") {
                UpdateBackInstance();
            }
        }

        public void UpdateBackInstance() {
            if (_fe.ModelInstanceBack == null) return;
            var ccback = GetTemplateChild("cpViewBack") as ContentControl;

            var b = ViewLocator.LocateForModel(_fe.ModelInstanceBack, null, null) as FrameworkElement;
            if (b == null) return;
            b.HorizontalAlignment = HorizontalAlignment.Stretch;
            b.VerticalAlignment = VerticalAlignment.Stretch;
            ViewModelBinder.Bind(_fe.ModelInstanceBack, b, null);
            if (ccback != null) ccback.Content = b;
        }

        private void CheckShare() {
            endPoints = new List<EndPoint>();
            foreach (var c in AppState.ShareContracts) {
                foreach (var ep in c.GetEndPoints(_fe.Contracts)) {
                    if (!_fe.Contracts.ContainsKey(ep.ContractType)) continue;
                    ep.Value = _fe.Contracts[ep.ContractType];
                    endPoints.Add(ep);
                }
            }
            _fe.CanStream = endPoints.Any();
        }

        private void Imb_ClientRemoved(object sender, ImbClientStatus e) {
            CheckShare();
            //if (_fe.AllowStream)
            //{
            //  if (AppState.Imb.ScreenshotReceivingClients.Count == 1 &&
            //    AppState.Imb.ScreenshotReceivingClients.Contains(e))
            //  {

            //  }
            //}
        }

        private void ClientsUpdated(object sender, ImbClientStatus e) {
            CheckShare();
        }

        private void UpdateStreamVisibility() {
            CheckShare();

            //_fe.CanStream = ((_fe.AllowStream && AppState.Imb != null && AppState.Imb.ScreenshotReceivingClients.Any()));
        }

        public void SetResize(FrameworkElement _resize) {
            var start = new Point();

            _resize.SetBinding(VisibilityProperty,
                new Binding {
                    Source = _fe,
                    Path = new PropertyPath("CanScale"),
                    Converter = new BooleanToVisibilityConverter()
                });

            _resize.MouseDown += (e, s) => {
                start = s.GetPosition(_svi);
                s.Handled = true;
                s.MouseDevice.Capture(_resize);
            };
            _resize.MouseMove += (e, s) => {
                if (s.LeftButton == MouseButtonState.Pressed) {
                    var p = s.GetPosition(_svi);
                    var difx = p.X - start.X;
                    var dify = p.Y - start.Y;
                    var nw = _svi.ActualWidth + difx;
                    if (nw > _fe.SwitchWidth) _svi.Width = nw;
                    _svi.Height = _svi.ActualHeight + dify;
                    start = p;
                    s.Handled = true;
                }
            };
            _resize.MouseUp += (e, s) => _resize.ReleaseMouseCapture();

            _resize.TouchDown += (e, s) => {
                start = s.GetTouchPoint(_svi).Position;
                s.Handled = true;
                s.TouchDevice.Capture(_resize);
            };

            _resize.TouchMove += (e, s) => {
                var p = s.GetTouchPoint(_svi).Position;
                var difx = p.X - start.X;
                var dify = p.Y - start.Y;
                var nw = _svi.ActualWidth + difx;
                if (nw > _fe.SwitchWidth) _svi.Width = nw;
                _svi.Height = _svi.ActualHeight + dify;
                start = p;
                s.Handled = true;
            };
        }

        public override void OnApplyTemplate() {
            //csCommon.Resources.FloatingStyles fs = new FloatingStyles();
            //var ct = fs.FindName("SimpleFloatingStyle") as ControlTemplate;

            base.OnApplyTemplate();
            if (DesignerProperties.GetIsInDesignMode(this)) return;

            AppStateSettings.Instance.FullScreenFloatingElementChanged += InstanceFullScreenFloatingElementChanged;


            if (_fe.Style != null) {
                SetBinding(StyleProperty,
                    new Binding {
                        Source = _fe,
                        Path = new PropertyPath("Style"),
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    });
            }

            //this.Style = this.FindResource("SimpleContainer") as Style;

            _svi.BorderThickness = new Thickness(0, 0, 0, 0);

            _svi.SingleInputRotationMode = (_fe.RotateWithFinger)
                ? SingleInputRotationMode.Default
                : SingleInputRotationMode.Disabled;

            _svi.ContainerManipulationDelta += _svi_ScatterManipulationDelta;


            _fe.ScatterViewItem = _svi;

            if (!_fe.ShowShadow) {
                SurfaceShadowChrome ssc;
                ssc = _svi.Template.FindName("shadow", _svi) as SurfaceShadowChrome;
                _svi.BorderBrush = null;
                _svi.Background = null;
                if (ssc != null) ssc.Visibility = Visibility.Hidden;
            }

            var closeButton = GetTemplateChild("PART_Close") as Button;
            if (closeButton != null)
                closeButton.Click += CloseButtonClick;

            _partAssociation = GetTemplateChild("PART_Association") as Line;
            _partContent = GetTemplateChild("PART_Content") as Grid;
            _partPreview = GetTemplateChild("PART_Preview") as FrameworkElement;
            _partStream = GetTemplateChild("PART_Stream") as SurfaceButton;
            _cpView = GetTemplateChild("cpView") as FrameworkElement;
            if (_fe == null) return;
            DataContext = _fe;

            _fe.CloseRequest += FeCloseRequest;
            _fe.ResetRequest += _fe_ResetRequest;


            _svi.SetBinding(WidthProperty,
                new Binding {Source = _fe, Path = new PropertyPath("Width"), Mode = BindingMode.Default});
            _svi.SetBinding(HeightProperty,
                new Binding {Source = _fe, Path = new PropertyPath("Height"), Mode = BindingMode.Default});

            _resize = GetTemplateChild("bResize") as Border;
            if (_resize != null) {
                SetResize(_resize);
            }

            UpdateAssociatedLine();

            var resizeBack = GetTemplateChild("bResize1") as Border;
            if (resizeBack != null) {
                SetResize(resizeBack);
            }

            _cc = GetTemplateChild("cpView") as ContentControl;

            // check for document, if not exist use ModelInstance
            if (_fe.Document != null) {
                IDocument vm = null;
                _fe.ConnectChannel = _fe.Document.Channel;
                _fe.ConnectMessage = _fe.Document.ToString();
                switch (_fe.Document.FileType) {
                    case FileTypes.image:
                        vm = new ImageViewModel {Doc = _fe.Document};
                        break;
                    case FileTypes.imageFolder:
                        vm = new ImageFolderViewModel {Doc = _fe.Document};
                        break;
                    case FileTypes.xps:
                        vm = new XpsViewModel {Doc = _fe.Document};
                        break;
                    case FileTypes.video:
                        vm = new VideoViewModel {Doc = _fe.Document};
                        break;
                    case FileTypes.web:
                        vm = new WebViewModel {Doc = _fe.Document};
                        break;
                    case FileTypes.html:
                        vm = new HtmlViewModel {Doc = _fe.Document};
                        break;
                }
                if (vm != null) {
                    var b = ViewLocator.LocateForModel(vm, null, null) as FrameworkElement;
                    if (b != null) {
                        b.Width = double.NaN;
                        b.Height = double.NaN;
                        b.HorizontalAlignment = HorizontalAlignment.Stretch;
                        b.VerticalAlignment = VerticalAlignment.Stretch;
                        ViewModelBinder.Bind(vm, b, null);
                        if (_cc != null) _cc.Content = b;
                    }
                }
            }
            else if (_fe.ModelInstance != null) {
                try {
                    var b = ViewLocator.LocateForModel(_fe.ModelInstance, null, null) as FrameworkElement;
                    if (b != null) {
                        b.HorizontalAlignment = HorizontalAlignment.Stretch;
                        b.VerticalAlignment = VerticalAlignment.Stretch;
                        ViewModelBinder.Bind(_fe.ModelInstance, b, null);
                        if (_cc != null) _cc.Content = b;
                    }
                }
                catch (Exception e) {
                    Logger.Log("Floating Container", "Error adding floating element", e.Message, Logger.Level.Error);
                }
            }

            UpdateBackInstance();

            if (_fe.DockingStyle == DockingStyles.None)
                if (_partPreview != null) _partPreview.Visibility = Visibility.Collapsed;

            _svi.PreviewTouchDown += (e, s) => {
                if (_svi.TouchesOver.Count() == 4) {
                    SwitchFullscreen();
                    s.Handled = true;
                    _svi.ReleaseAllTouchCaptures();
                }
                //return;
                // FIXME TODO: Unreachable code
//                if (!InteractiveSurface.PrimarySurfaceDevice.IsFingerRecognitionSupported)
//                    return;
//
//
//                _touchDown = DateTime.Now;
//                if (s.TouchDevice.GetIsFingerRecognized()) {
//                    _fe.OriginalOrientation = s.Device.GetOrientation(this);
//                    //double angle = s.TouchDevice.GetOrientation(Application.Current.MainWindow);
//                    //_reverse = (angle < 180);
//                }
//
//                if (!s.TouchDevice.GetIsFingerRecognized() &&
//                    !s.TouchDevice.GetIsTagRecognized()) {
//                    if (!string.IsNullOrEmpty(_fe.ConnectChannel) &&
//                        DateTime.Now > _lastBlobEvent.AddSeconds(1)) {
//                        AppStateSettings.Instance.Imb.SendMessage(_fe.ConnectChannel,
//                            _fe.ConnectMessage);
//                        s.Handled = true;
//                        _lastBlobEvent = DateTime.Now;
//                    }
//                }


                //Console.WriteLine(d.ToString());
            };
            //_svi.PreviewTouchUp += (e, s) =>
            //            {
            //              if (!_fe.Large && _touchDown.AddMilliseconds(300) > DateTime.Now && _fe.LastContainerPosition!=null)
            //              {
            //                ResetLastPosition();
            //              }
            //            };

            _svi.PreviewMouseDown += (e, s) => { _touchDown = DateTime.Now; };
            _svi.PreviewMouseUp += (e, s) => {
                if (!_fe.Large && _touchDown.AddMilliseconds(300) > DateTime.Now &&
                    _fe.LastContainerPosition != null) {
                    ResetLastPosition();
                }
            };

            if (_fe.IsFullScreen) {
                FrameworkElement fe = Application.Current.MainWindow;
                _svi.Center = new Point(fe.Width/2, fe.Height/2);
                _svi.Width = fe.Width;
                _svi.Height = fe.Height;
                _svi.Opacity = _fe.OpacityNormal;
            }

            if (_partStream != null) {
                _partStream.Click += _partStream_Click;
            }

            //b.HorizontalAlignment = HorizontalAlignment.Stretch;
            //b.VerticalAlignment = VerticalAlignment.Stretch;
        }

        private void UpdateAssociatedLine() {
            if (_partAssociation == null || _fe.AssociatedPoint == null) return;
            var p = AppState.ViewDef.MapPoint(_fe.AssociatedPoint);
            p.X += ActualWidth/2;
            p.Y += ActualHeight/2;
            var p2 = AppState.ViewDef.MapControl.TranslatePoint(p, _partContent);
            _partAssociation.X1 = p2.X - ActualWidth/2;
            _partAssociation.X2 = ActualWidth/2;
            _partAssociation.Y1 = p2.Y - ActualHeight/2;
            _partAssociation.Y2 = ActualHeight/2;
        }

        private void _fe_ResetRequest(object sender, EventArgs e) {
            Reset();
        }

        private void _partStream_Click(object sender, RoutedEventArgs e) {
            var menu = new MenuPopupViewModel {
                RelativeElement = _partStream,
                RelativePosition = new Point(0, 0),
                TimeOut = new TimeSpan(0, 0, 0, 5),
                VerticalAlignment = VerticalAlignment.Top,
                AutoClose = true
            };
            //menu.Point = _view.CreateLayer.TranslatePoint(new Point(0,0),Application.Current.MainWindow);
            //menu.DisplayProperty = "ServiceName";
            menu.Selected += MenuSelected;
            foreach (var ep in endPoints) {
                var mi = menu.AddMenuItem(ep.Title);
                mi.Tag = ep;
            }
            //foreach (ImbClientStatus a in AppStateSettings.Instance.Imb.Clients.Values.Where(k => k.Client))
            //{
            //  System.Windows.Controls.MenuItem mi = menu.AddMenuItem(a.Name);
            //  mi.Tag = a;
            //}
            //var config = menu.AddMenuItem("Configuration");

            AppStateSettings.Instance.Popups.Add(menu);
        }

        private void MenuSelected(object sender, MenuSelectedEventArgs e) {
            var endpoint = e.Object as EndPoint;
            if (endpoint != null) {
                endpoint.Contract.Send(endpoint, this);
            }
        }

        private void InstanceFullScreenFloatingElementChanged(object sender, EventArgs e) {
            Visibility = (sender == null || sender == _fe) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ResetLastPosition() {
            //Duration d = new Duration(new TimeSpan(0,0,0,0,500));
            //_svi.BeginAnimation(ScatterViewItem.WidthProperty, new StopHoldDoubleAnimation(_fe.LastContainerPosition.Size.Width, d, _svi, ScatterViewItem.WidthProperty) { FillBehavior = FillBehavior.Stop });
            //_svi.BeginAnimation(ScatterViewItem.HeightProperty, new StopHoldDoubleAnimation(_fe.LastContainerPosition.Size.Height, d, _svi, ScatterViewItem.HeightProperty) { FillBehavior = FillBehavior.Stop });
            //_svi.BeginAnimation(ScatterViewItem.OrientationProperty, new StopHoldDoubleAnimation(_fe.LastContainerPosition.Orientation, d, _svi, ScatterViewItem.OrientationProperty) { FillBehavior = FillBehavior.Stop });
            //_svi.BeginAnimation(ScatterViewItem.CenterProperty, new StopHoldPointAnimation(_fe.LastContainerPosition.Center, d, _svi, ScatterViewItem.CenterProperty) { FillBehavior = FillBehavior.Stop });
            _svi.Width = _fe.LastContainerPosition.Size.Width;
            _svi.Height = _fe.LastContainerPosition.Size.Height;
            _svi.Orientation = _fe.LastContainerPosition.Orientation;
            _svi.Center = _fe.LastContainerPosition.Center;
        }

        // Return the result of the hit test to the callback.
        public HitTestResultBehavior MyHitTestResult(HitTestResult result) {
            // Add the hit test result to the list that will be processed after the enumeration.
            var hit = result.VisualHit as FrameworkElement;
            if (hit == null) return HitTestResultBehavior.Continue;
            var fe = hit;
            if (!(fe.DataContext is FloatingElement) || fe.DataContext == _fe)
                return HitTestResultBehavior.Continue;
            var f = (FloatingElement) fe.DataContext;
            if (f.AllowDrop && f.AllowedDropsTags.Contains(_fe.DropTag)) {
                hitResultsList.Add(f);
            }

            // Set the behavior to return visuals at all z-order levels.
            return HitTestResultBehavior.Continue;
        }


        private void _svi_ScatterManipulationDelta(object sender, ContainerManipulationDeltaEventArgs e) {
            if (!_fe.CanDrag) return;
            hitResultsList = new List<FloatingElement>();
            // Set up a callback to receive the hit test result enumeration,
            // but no hit test filter enumeration.
            VisualTreeHelper.HitTest(Application.Current.MainWindow,
                null, // No hit test filtering.
                MyHitTestResult,
                new PointHitTestParameters(_svi.Center));

            if (hitResultsList != null && hitResultsList.Count > 0) {
                var fe = hitResultsList[0];
                if (_lastDragElement == null) {
                    fe.ForceDragEnter(this, _fe);
                }
                else {
                    if (fe != _lastDragElement) {
                        _lastDragElement.ForceDragLeave(this, _fe);
                        fe.ForceDragEnter(this, fe);
                    }
                }

                _lastDragElement = fe;
            }
            else {
                if (_lastDragElement != null) {
                    _lastDragElement.ForceDragLeave(this, _fe);
                }
                _lastDragElement = null;
            }
        }


        private void FeCloseRequest(object sender, EventArgs e) {
            RaiseEvent(new RoutedEventArgs(CloseEvent, this));
        }


        private void InitScatterViewEvents() {
            _fe = (FloatingElement) DataContext;
            //if (_fe.Style == null) _fe.Style = this.FindResource("DefaultContainerStyle") as Style;
            _svi = (ScatterViewItem) Helpers.FindElementOfTypeUp(this, typeof (ScatterViewItem));

            if (_fe == null || _svi == null) return;
            _fe.PropertyChanged += FePropertyChanged;

            _floatingViewModel = IoC.Get<IFloating>();

            //TODO: get scatterview instead of window
            _sv = Application.Current.MainWindow;
            // (ScatterView)BaseWPFHelpers.Helpers.FindElementOfTypeUp(_svi, typeof(ScatterView));

            _fe.FlipEvent += FeFlipEvent;
            _fe.UnFlipEvent += FeUnFlipEvent;


            _svi.IsManipulationEnabled = !_fe.PromoteToMouse;

            _svi.ContainerManipulationCompleted += SviScatterManipulationCompleted;
            _svi.ContainerManipulationStarted += SviScatterManipulationStarted;
            _svi.ContainerManipulationDelta += SviScatterManipulationDelta;
            _svi.TouchDown += SviContactDown;
            //_svi.CanMove = false;

            _fe.DockingStyleChanged += _fe_DockingStyleChanged;

            SizeChanged += UcFloatingElementContainerSizeChanged;

            _svi.SetBinding(ScatterContentControlBase.CanMoveProperty,
                new Binding {Source = _fe, Path = new PropertyPath("CanMove")});
            _svi.SetBinding(ScatterContentControlBase.CanRotateProperty,
                new Binding {Source = _fe, Path = new PropertyPath("CanRotate")});
            _svi.SetBinding(ScatterContentControlBase.CanScaleProperty,
                new Binding {Source = _fe, Path = new PropertyPath("CanScale")});
            _svi.SetBinding(ScatterContentControlBase.ShowsActivationEffectsProperty,
                new Binding {Source = _fe, Path = new PropertyPath("ShowsActivationEffects")});

            if (_fe.DockingStyle != DockingStyles.None)
                _svi.SetBinding(VisibilityProperty,
                    new Binding {
                        Source = AppStateSettings.Instance,
                        Path = new PropertyPath("DockedFloatingElementsVisible"),
                        Converter = new BooleanToVisibilityConverter()
                    });


            //_svi.CanMove = false;
            //_svi.CanScale = false;
            //_svi.CanRotate = false;

            Loaded += FloatingContainerLoaded;

            Close += FloatingContainerClose;

            PreviewMouseDoubleClick += FloatingContainerPreviewMouseDoubleClick;
        }

        private void _fe_DockingStyleChanged(object sender, EventArgs e) {
            if (_fe.DockingStyle != DockingStyles.LeftFix) return;
            //var b = _cc.Content;
            _cc.Content = null;
        }

        private void FeUnFlipEvent(object sender, EventArgs e) {
            var plane = (FrameworkElement) GetTemplateChild("Plane");
            if (plane == null) return;
            var sb = plane.FindResource("UnFlip") as Storyboard;
            if (sb != null) sb.Begin(plane);
        }

        private void FeFlipEvent(object sender, EventArgs e) {
            UpdateBackInstance();
            var plane = (FrameworkElement) GetTemplateChild("Plane");
            if (plane == null) return;
            var sb = plane.FindResource("Flip") as Storyboard;
            if (sb != null) sb.Begin(plane);
        }


        private void FloatingContainerPreviewMouseDoubleClick(object sender, MouseButtonEventArgs e) {
            SwitchFullscreen();
        }

        private void SwitchFullscreen() {
            if (!_fe.CanFullScreen) return;
            if (AppStateSettings.Instance.FullScreenFloatingElement == _fe) {
                AppStateSettings.Instance.FullScreenFloatingElement = null;
                _svi.Center = _previousCenter;
                _svi.Height = _previousHeight;
                _svi.Width = _previousWidth;

                if (_fe.FullScreenLock) {
                    _svi.CanMove = _fe.CanMove;
                    _svi.CanScale = _fe.CanScale;
                    _svi.CanRotate = _fe.CanScale;
                }

                _svi.Orientation = _previousAngle;
                Style = _previousStyle;
                VisualStateManager.GoToState(this, "Default", false);
                ApplyTemplate();
            }
            else {
                _previousStyle = Style;
                _previousCenter = _svi.Center;
                _previousHeight = _svi.ActualHeight;
                _previousWidth = _svi.ActualWidth;
                _previousAngle = _svi.ActualOrientation;
                if (_fe.FullScreenLock) {
                    _svi.CanMove = false;
                    _svi.CanRotate = false;
                    _svi.CanScale = false;
                }

                AppStateSettings.Instance.FullScreenFloatingElement = _fe;
                VisualStateManager.GoToState(this, "FullScreen", false);
                //this.Style = this.FindResource("NoBorder") as Style;
                FrameworkElement fe = Application.Current.MainWindow;


                //Animate(new Point(fe.Width / 2, fe.Height / 2), fe.Width, fe.Height, 0,0);  

                _svi.Opacity = _fe.OpacityNormal;
                _svi.Center = new Point(fe.Width/2, fe.Height/2);
                _svi.Width = fe.Width;
                _svi.Height = fe.Height;
                _svi.Orientation = 0;
            }
        }

        private void Animate(Point center, double width, double height, double orientation, double duration) {
            var pa = new StopHoldPointAnimation(center,
                new Duration(TimeSpan.FromMilliseconds(duration)),
                _svi, ScatterContentControlBase.CenterProperty);

            _svi.BeginAnimation(ScatterContentControlBase.CenterProperty, pa);

            var wa = new StopHoldDoubleAnimation(width,
                new Duration(TimeSpan.FromMilliseconds(duration)),
                _svi, WidthProperty);

            _svi.BeginAnimation(WidthProperty, wa);

            var ha = new StopHoldDoubleAnimation(height,
                new Duration(TimeSpan.FromMilliseconds(duration)),
                _svi, HeightProperty);

            _svi.BeginAnimation(HeightProperty, ha);

            var oa = new StopHoldDoubleAnimation(orientation,
                new Duration(TimeSpan.FromMilliseconds(duration)),
                _svi, ScatterContentControlBase.OrientationProperty);

            _svi.BeginAnimation(ScatterContentControlBase.OrientationProperty, oa);
        }

        private void FloatingContainerLoaded(object sender, RoutedEventArgs e) {
            if (_fe.StartSize.HasValue) {
                _svi.SetBinding(WidthProperty,
                    new Binding {Source = _fe.StartSize.Value, Path = new PropertyPath("Width")});
                _svi.SetBinding(HeightProperty,
                    new Binding {Source = _fe.StartSize.Value, Path = new PropertyPath("Height")});
                //_svi.Width = _fe.StartSize.Value.Width;
                //_svi.Height = _fe.StartSize.Value.Height;
            }
            else {
                _svi.SetBinding(WidthProperty, new Binding {Source = _fe.Width, Path = new PropertyPath("Width")});
                _svi.SetBinding(HeightProperty, new Binding {Source = _fe.Height, Path = new PropertyPath("Height")});
                //if (_fe.Width > 0) _svi.Width = _fe.Width;
                //if (_fe.Height > 0) _svi.Height = _fe.Height;
            }

            if (_fe.OriginPosition.HasValue || _fe.OriginSize.HasValue) {
                var d = new Duration(_fe.AnimationSpeed);
                if (_fe.StartPosition != null) {
                    var pa = new StopHoldPointAnimation(_fe.StartPosition.Value, d, _svi, ScatterContentControlBase.CenterProperty) {
                            FillBehavior = FillBehavior.Stop,
                            From = _fe.OriginPosition
                        };
                    _svi.BeginAnimation(ScatterContentControlBase.CenterProperty, pa);
                }

                if (_fe.OriginSize.HasValue) {
                    var wa = new StopHoldDoubleAnimation(_fe.StartSize.Value.Width, d, _svi, WidthProperty)
                    {FillBehavior = FillBehavior.Stop, From = _fe.OriginSize.Value.Width};
                    _svi.BeginAnimation(WidthProperty, wa);

                    var ha = new StopHoldDoubleAnimation(_fe.StartSize.Value.Height, d, _svi, HeightProperty)
                    {FillBehavior = FillBehavior.Stop, From = _fe.OriginSize.Value.Height};
                    _svi.BeginAnimation(HeightProperty, ha);
                }
            }
            else {
                if (_fe.StartPosition.HasValue) _svi.Center = _fe.StartPosition.Value;
            }

            if (_fe.StartOrientation.HasValue) _svi.Orientation = _fe.StartOrientation.Value;

            //if (_fe.StartSize.HasValue)
            //{
            //  _svi.Width = _fe.StartSize.Value.Width;
            //  _svi.Height = _fe.StartSize.Value.Height;
            //}
            //else
            //{
            //  if (_fe.Width > 0) _svi.Width = _fe.Width;
            //  if (_fe.Height > 0) _svi.Height = _fe.Height;
            //}

            try {
                if (_fe.MinSize.HasValue) {
                    if (!double.IsNaN(_fe.MinSize.Value.Width)) _svi.MinWidth = _fe.MinSize.Value.Width;
                    if (!double.IsNaN(_fe.MinSize.Value.Height)) _svi.MinHeight = _fe.MinSize.Value.Height;
                }

                if (_fe.MaxSize.HasValue) {
                    _svi.MaxWidth = _fe.MaxSize.Value.Width;
                    _svi.MaxHeight = _fe.MaxSize.Value.Height;
                }
            }
            catch (Exception) {
                Logger.Log("FrameworkContainer", "Error setting min/max size", "", Logger.Level.Error);
            }

            _svi.SetBinding(OpacityProperty, new Binding {Source = _fe, Path = new PropertyPath("OpacityNormal")});
        }

        private void FePropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "StartPosition") {
                if (!Undocked) Dock();
            }
            if (e.PropertyName == "Icon") {}
            //OnApplyTemplate();
        }


        private static void SviContactDown(object sender, TouchEventArgs e) {}

        private void FloatingContainerClose(object sender, RoutedEventArgs e) {
            if (_fe.IsFullScreen) AppStateSettings.Instance.FullScreenFloatingElement = null;
            if (_fe.DockingStyle == DockingStyles.None) {
                var da = new DoubleAnimation(0, new Duration(new TimeSpan(0, 0, 0, 0, 200)));
                da.Completed += (f, s) => {
                    _svi.ReleaseAllTouchCaptures();
                    _svi.ReleaseAllTouchCaptures();
                    _floatingViewModel.AppStateSettings.FloatingItems.RemoveFloatingElement(_fe);
                };
                BeginAnimation(OpacityProperty, da);
            }
            else {
                Dock();
            }
        }

        private void Dock() {
            _fe.Large = false;
            Undocked = false;
            _fe.TriggerClosedEvent();

            if (_fe.StartPosition != null) {
                var spa = new StopHoldPointAnimation(_fe.StartPosition.Value, new Duration(_fe.AnimationSpeed), _svi,
                    ScatterContentControlBase.CenterProperty)
                {FillBehavior = FillBehavior.Stop};
                spa.Completed += SpaCompleted;

                _svi.BeginAnimation(ScatterContentControlBase.CenterProperty, spa);
            }
            if (_fe.StartSize != null) {
                _svi.BeginAnimation(WidthProperty,
                    new StopHoldDoubleAnimation(_fe.StartSize.Value.Width,
                        new Duration(_fe.AnimationSpeed),
                        _svi, WidthProperty) {FillBehavior = FillBehavior.Stop});

                _svi.BeginAnimation(HeightProperty,
                    new StopHoldDoubleAnimation(_fe.StartSize.Value.Height,
                        new Duration(_fe.AnimationSpeed),
                        _svi, HeightProperty));
            }
            if (_svi.Orientation < 180) {
                if (_fe.StartOrientation != null)
                    _svi.BeginAnimation(ScatterContentControlBase.OrientationProperty,
                        new StopHoldDoubleAnimation(_fe.StartOrientation.Value,
                            new Duration(_fe.AnimationSpeed), _svi,
                            ScatterContentControlBase.OrientationProperty)
                        {FillBehavior = FillBehavior.Stop});
            }
            else {
                if (_fe.StartOrientation != null)
                    _svi.BeginAnimation(ScatterContentControlBase.OrientationProperty,
                        new StopHoldDoubleAnimation(_fe.StartOrientation.Value + 360,
                            new Duration(_fe.AnimationSpeed), _svi,
                            ScatterContentControlBase.OrientationProperty)
                        {FillBehavior = FillBehavior.Stop});
            }
        }

        private void Reset() {
            if (_fe.StartPosition != null) {
                //var spa = new StopHoldPointAnimation(_fe.StartPosition.Value, new Duration(_fe.AnimationSpeed), _svi,
                //                   ScatterContentControlBase.CenterProperty) { FillBehavior = FillBehavior.Stop };

                //_svi.BeginAnimation(ScatterContentControlBase.CenterProperty, spa);
                _svi.Center = _fe.StartPosition.Value;
            }
        }

        private void SpaCompleted(object sender, EventArgs e) {
            Undocked = false;
            //_reverse = false;
            //RenderTransform = new RotateTransform((_reverse) ? 180 : 0);
            CheckSize();
        }


        private void CheckSize() {
            if (_fe.IsFullScreen) return;
            var oldView = _fe.Large;
            _fe.Large = (_svi.ActualWidth > _fe.SwitchWidth);

            if (_fe.DockingStyle == DockingStyles.None) {
                //IsContained = _fe.Contained;
            }
            else {
                if (_fe.Large) {
                    if (!oldView && _partPreview != null) {
                        _partPreview.Visibility = Visibility.Collapsed;
                        _partContent.Visibility = Visibility.Visible;
                        // _svi.Orientation = _fe.OriginalOrientation;

                        Undocked = true;
                    }
                }
                if (_fe.Large || _partPreview == null) return;
                RenderTransformOrigin = new Point(0.5, 0.5);
                RenderTransform = new RotateTransform((_reverse) ? 180 : 0);
                _partPreview.Visibility = Visibility.Visible;
                _partContent.Visibility = Visibility.Collapsed;
            }
        }

        private void CloseButtonClick(object sender, RoutedEventArgs e) {
            // save last position
            if (_fe.CanFullScreen)
                _fe.CanFullScreen = false;
            _fe.LastContainerPosition = new ContainerPosition {
                Size = new Size(ActualWidth, ActualHeight),
                Center = _svi.Center,
                Orientation = _svi.ActualOrientation
            };
            _fe.TriggerClosedEvent();

            RaiseEvent(new RoutedEventArgs(CloseEvent, this));
        }

        private void SviScatterManipulationStarted(object sender, ContainerManipulationStartedEventArgs e) {
            if (_fe.IsFullScreen) return;
            _svi.Opacity = _fe.OpacityDragging;
        }

        private void UcFloatingElementContainerSizeChanged(object sender, SizeChangedEventArgs e) {
            if (_fe.DockingStyle != DockingStyles.None) {
                CheckSize();
            }
            if (_fe.AspectSize.HasValue && _fe.Large) {
                _svi.Height = (_svi.Width/_fe.AspectSize.Value.Width)*_fe.AspectSize.Value.Height;
            }
        }

        private void SviScatterManipulationDelta(object sender, ContainerManipulationDeltaEventArgs e) {
            if (_fe.IsFullScreen) return;
            //UpdateAssociatedLine();
            if (_fe == null || _fe.StartSize == null) return;
            if ((_fe.DockingStyle == DockingStyles.Right) &&
                (_svi.Center.X < Application.Current.MainWindow.ActualWidth - _fe.StartSize.Value.Width) &&
                !_fe.Large && !Undocked) {
                var delta = 1 +
                            (Application.Current.MainWindow.ActualWidth - _fe.StartSize.Value.Width -
                             _svi.Center.X)/_fe.DragScaleFactor;


                if (_fe.StartSize != null && !double.IsInfinity(delta)) {
                    _svi.Width = _fe.StartSize.Value.Width*delta;
                    _svi.Height = _fe.StartSize.Value.Height*delta;
                }
            }


            if ((_fe.DockingStyle == DockingStyles.Left) && (_svi.Center.X > _fe.StartSize.Value.Width) &&
                !_fe.Large && !Undocked) {
                var deltax = 1 + (_svi.Center.X - _fe.StartSize.Value.Width)/_fe.DragScaleFactor;
                var deltay = deltax;

                if (_fe.TargetSize.HasValue) {
                    if (_fe.SwitchWidth == _fe.TargetSize.Value.Width) {
                        deltay = ((_fe.TargetSize.Value.Height/_fe.StartSize.Value.Height)/
                                  (_fe.TargetSize.Value.Width/_fe.StartSize.Value.Width)*deltax);
                    }
                }

                if (_fe.StartSize != null && !double.IsInfinity(deltax)) {
                    _svi.Width = _fe.StartSize.Value.Width*deltax;
                    _svi.Height = _fe.StartSize.Value.Height*deltay;
                }
            }

            if ((_fe.DockingStyle == DockingStyles.Up) && (_svi.Center.Y > _fe.StartSize.Value.Width) &&
                !_fe.Large && !Undocked) {
                var deltax = 1 + (_svi.Center.Y - _fe.StartSize.Value.Width)/_fe.DragScaleFactor;
                var deltay = deltax;

                if (_fe.TargetSize.HasValue) {
                    if (_fe.SwitchWidth == _fe.TargetSize.Value.Width) {
                        deltay = ((_fe.TargetSize.Value.Height/_fe.StartSize.Value.Height)/
                                  (_fe.TargetSize.Value.Width/_fe.StartSize.Value.Width)*deltax);
                    }
                }

                if (_fe.StartSize != null && !double.IsInfinity(deltax)) {
                    _svi.Width = _fe.StartSize.Value.Width*deltax;
                    _svi.Height = _fe.StartSize.Value.Height*deltay;
                }
            }
        }

        private void SviScatterManipulationCompleted(object sender, ContainerManipulationCompletedEventArgs e) {
            if (_fe.IsFullScreen) return;
            _svi.Opacity = _fe.OpacityNormal;
            const double dockwidth = 75;
            const double edge = 25;

            if (_fe.RemoveOnEdge) {
                if (_svi.Center.X > _sv.ActualWidth - edge || _svi.Center.X < edge ||
                    _svi.Center.Y > _sv.ActualHeight - edge || _svi.Center.Y < edge) {
                    RaiseEvent(new RoutedEventArgs(CloseEvent, this));
                    _fe.TriggerClosedEvent();
                }
            }

            if (_fe.ResetOnEdge) {
                if (_svi.Center.X > _sv.ActualWidth - edge || _svi.Center.X < edge ||
                    _svi.Center.Y > _sv.ActualHeight - edge || _svi.Center.Y < edge) {
                    RaiseEvent(new RoutedEventArgs(ResetEvent, this));
                    _fe.Reset();
                }
            }

            if (_fe.DockingStyle == DockingStyles.Right && _svi.Center.X > _sv.ActualWidth - dockwidth) Dock();
            if (_fe.DockingStyle == DockingStyles.Left && _svi.Center.X < dockwidth) Dock();
            if (_fe.DockingStyle == DockingStyles.Up && _svi.Center.Y < dockwidth) Dock();

            if (_lastDragElement != null) _lastDragElement.ForceDrop(this, _fe);
        }
    }
}