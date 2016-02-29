using BaseWPFHelpers;
using csMapCustomControls.MapIconMenu;
using MenuKiller;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace csCommon.csMapCustomControls.MapIconMenu
{
    /// <summary>
    /// NOTE:
    /// The MapMenuItem 
    /// IS a ICustomAlignedControl, and due to its template, it also
    /// HAS an ICustomAlignedControl as its only child, which will, in fact, do the measure/arrange for us.
    /// </summary>
    [TemplatePart(Name = "PART_Button", Type = typeof(Button))]
    [TemplatePart(Name = "PART_Panel", Type = typeof(CircularPanel))]
    [TemplatePart(Name = "PART_AlignPanel", Type = typeof(ReferenceAlignPanel))]
    public class MapMenuItem : TreeViewItem, ICommandSource, ICustomAlignedControl
    {
        static MapMenuItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MapMenuItem), new FrameworkPropertyMetadata(typeof(MapMenuItem)));

            var binding = new CommandBinding(MapMenuCommands.ToggleExpansion);
            var bindingExpand = new CommandBinding(MapMenuCommands.Expand);
            binding.Executed += ToggleCommandHandler;
            binding.CanExecute += CanToggleHandler;

            bindingExpand.Executed += BindingExpandExecuted;
            bindingExpand.CanExecute += CanExpandHandler;

            CommandManager.RegisterClassCommandBinding(typeof(MapMenuItem), binding);
            CommandManager.RegisterClassCommandBinding(typeof(MapMenuItem), bindingExpand);
        }

        public FrameworkElement RelativeElement;
        public event IconMovedEventHandler IconMoved;
        public event IconTapedEventHandler IconTapped;
        public event IconMovedEventHandler IconReleased;
        public event IconMovedEventHandler IconStartMoving;
        public event IconMovedEventHandler IconMoveCompleted;

        private DispatcherTimer timeoutTimer;
        private DateTime mouseTime;
        private DateTime touchTime;

        public bool CallOutEnabled { get; set; }

        public TimeSpan CallOutTimeOut
        {
            get { return (TimeSpan)GetValue(CallOutTimeOutProperty); }
            set { SetValue(CallOutTimeOutProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CallOutTimeOut.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CallOutTimeOutProperty =
            DependencyProperty.Register("CallOutTimeOut", typeof(TimeSpan), typeof(MapMenuItem), new UIPropertyMetadata(new TimeSpan(0, 0, 0, 10)));

        public string Id { get; set; }

        private BitmapImage _image;
        public BitmapImage Image
        {
            get { return _image; }
            set { _image = value; }
        }

        public bool CallOutVisible
        {
            get { return (bool)GetValue(CallOutVisibleProperty); }
            set
            {
                SetValue(CallOutVisibleProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for CallOutVisible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CallOutVisibleProperty =
            DependencyProperty.Register("CallOutVisible", typeof(bool), typeof(MapMenuItem), new UIPropertyMetadata(false));

        public bool Moving
        {
            get { return (bool)GetValue(MovingProperty); }
            set { SetValue(MovingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Moving.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MovingProperty =
            DependencyProperty.Register("Moving", typeof(bool), typeof(MapMenuItem), new UIPropertyMetadata(false));

        // Executed event handler.
        private static void BindingExpandExecuted(object target, ExecutedRoutedEventArgs e)
        {
            var item = target as MapMenuItem;
            if (item != null)
            {
                item.Expand();
            }
        }

        // CanExecute event handler.
        private static void CanExpandHandler(object target, CanExecuteRoutedEventArgs e)
        {
            var item = target as MapMenuItem;
            if (item != null)
            {
                e.CanExecute = item.CanToggleExpand();
            }
        }

        #region Private Members

        private Button _mCenterButton;
        private CircularPanel _mPanel;
        private ReferenceAlignPanel _mAlignPanel;

        #endregion

        #region Attached Event MouseHover




        public static readonly RoutedEvent MouseHoverEvent =
            EventManager.RegisterRoutedEvent("MouseHover",
                                             RoutingStrategy.Bubble,
                                             typeof(RoutedEventHandler),
                                             typeof(MapMenuItem));

        public static void AddMouseHoverHandler(DependencyObject o, RoutedEventHandler handler)
        {
            // ((UIElement)o).AddHandler(MapMenuItem.MouseHoverEvent, handler);
        }

        public static void RemoveMouseHoverHandler(DependencyObject o, RoutedEventHandler handler)
        {
            //((UIElement)o).RemoveHandler(MapMenuItem.MouseHoverEvent, handler);
        }

        #endregion

        #region ICommandSource Dependency Properties

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                "Command",
                typeof(ICommand),
                typeof(MapMenuItem),
                new PropertyMetadata(null,
                                     CommandChanged));

        [Description("Assigns a Command to this MapMenuItem executed upon button click unless this item has children.")]
        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public static readonly DependencyProperty CommandTargetProperty =
            DependencyProperty.Register(
                "CommandTarget",
                typeof(IInputElement),
                typeof(MapMenuItem),
                new PropertyMetadata((IInputElement)null));

        [Description("Assigns a CommandTarget to this MapMenuItem to apply the Command to.")]
        public IInputElement CommandTarget
        {
            get { return (IInputElement)GetValue(CommandTargetProperty); }
            set { SetValue(CommandTargetProperty, value); }
        }

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(
                "CommandParameter",
                typeof(object),
                typeof(MapMenuItem),
                new PropertyMetadata((object)null));

        [Description("Assigns a CommandParameter to this MapMenuItem which will be passed when executing Command.")]
        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        #endregion

        public static readonly DependencyProperty RootToolTipProperty =
            DependencyProperty.Register(
                "RootToolTip",
                typeof(object),
                typeof(MapMenuItem),
                new PropertyMetadata(null));

        [Description(
            "Used to retrieve the RootToolTip of the MapMenuItem which is currently or was most recently under the mouse."
            )]
        public object RootToolTip
        {
            get { return GetValue(RootToolTipProperty); }
            set { SetValue(RootToolTipProperty, value); }
        }

        #region CommandChanges

        private static void CommandChanged(DependencyObject d,
                                           DependencyPropertyChangedEventArgs e)
        {
            var mki = (MapMenuItem)d;
            mki.HookUpCommand((ICommand)e.OldValue, (ICommand)e.NewValue);
        }

        private void HookUpCommand(ICommand oldCommand, ICommand newCommand)
        {
            if (oldCommand != null)
            {
                RemoveCommand(oldCommand, newCommand);
            }

            AddCommand(oldCommand, newCommand);
        }

        private void RemoveCommand(ICommand oldCommand, ICommand newCommand)
        {
            EventHandler handler = CanExecuteChanged;
            //oldCommand.CanExecuteChanged -= handler;
        }

        private void AddCommand(ICommand oldCommand, ICommand newCommand)
        {
            EventHandler handler = CanExecuteChanged;
            canExecuteChangedHandler = handler;
            if (newCommand != null)
            {
                //newCommand.CanExecuteChanged += _canExecuteChangedHandler;
            }
        }

        private void CanExecuteChanged(object sender, EventArgs e)
        {
            if (Command == null) return;
            var command = Command as RoutedCommand;

            if (command != null)
            {
                if (command.CanExecute(CommandParameter, CommandTarget))
                {
                    IsEnabled = true;
                }
                else
                {
                    IsEnabled = true;
                }
            }
            else
            {
                if (Command.CanExecute(CommandParameter))
                {
                    IsEnabled = true;
                }
                else
                {
                    IsEnabled = true;
                }
            }
        }

        private static EventHandler canExecuteChangedHandler;

        // Create a custom routed event by first registering a RoutedEventID
        // This event uses the bubbling routing strategy
        public static readonly RoutedEvent TapEvent = EventManager.RegisterRoutedEvent(
            "Tap", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MapMenuItem));

        // Provide CLR accessors for the event
        public event RoutedEventHandler Tap
        {
            add { AddHandler(TapEvent, value); }
            remove { RemoveHandler(TapEvent, value); }
        }

        // Create a custom routed event by first registering a RoutedEventID
        // This event uses the bubbling routing strategy
        public static readonly RoutedEvent ReleasedEvent = EventManager.RegisterRoutedEvent(
            "Released", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MapMenuItem));

        // Provide CLR accessors for the event
        public event RoutedEventHandler Released
        {
            add { AddHandler(ReleasedEvent, value); }
            remove { RemoveHandler(ReleasedEvent, value); }
        }



        // This method raises the Tap event
        void RaiseTapEvent()
        {
            var newEventArgs = new RoutedEventArgs(MapMenuItem.TapEvent);
            RaiseEvent(newEventArgs);
            if (!CallOutEnabled) return;
            CallOutVisible = !CallOutVisible;
            if (!CallOutVisible) return;
            if (!(CallOutTimeOut.TotalMilliseconds > 0)) return;
            var dt = new DispatcherTimer(DispatcherPriority.Background) { Interval = CallOutTimeOut };
            dt.Tick += (es, s) =>
            {
                CallOutVisible = false;
                dt.Stop();
            };
            dt.Start();
        }

        private DateTime _lastTouch = DateTime.Now;

        private MapMenu mm;

        #endregion

        //private static void MenuKillerItemMouseEnter(object o, MouseEventArgs e)
        //{
        //    var sender = o as Button;
        //}

        private bool isTouch;
        //private DateTime lastTouch;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // NOTE: Do not use GetTemplateChild(), because it will only find direct descendants.
            // Also refer to the MSDN Comment of GetTemplateChild() (shown in IntelliSense).
            // This is a common mistake.
            //
            // NOTE: FindName() also exist for this (from FrameworkElement), but we want to find
            // on the _template_
            _mCenterButton = Template.FindName("PART_Button", this) as Button;

            if (null != _mCenterButton)
            {
                _mCenterButton.Click += MCenterButtonClick;
                //_mCenterButton.MouseEnter += MenuKillerItemMouseEnter;
                _mCenterButton.PreviewMouseDown += MCenterButtonPreviewMouseDown;
                _mCenterButton.PreviewMouseUp += MCenterButtonPreviewMouseUp;
                _mCenterButton.PreviewMouseMove += MCenterButtonPreviewMouseMove;
                _mCenterButton.MouseEnter += MCenterButtonMouseEnter;
                _mCenterButton.LostMouseCapture += MCenterButtonLostMouseCapture;

                _mCenterButton.PreviewTouchDown += MCenterButtonPreviewTouchDown;
                _mCenterButton.LostTouchCapture += MCenterButtonLostTouchCapture;
                _mCenterButton.PreviewTouchMove += MCenterButtonPreviewTouchMove;
            }

            _mPanel = Template.FindName("PART_Panel", this) as CircularPanel;

            _mAlignPanel = Template.FindName("PART_AlignPanel", this) as ReferenceAlignPanel;

            if (null != _mPanel)
            {
                _mPanel.ChildArranged += mPanel_ChildArranged;
            }
            SetLastTouch();

            mm = Helpers.FindElementOfTypeUp(this, typeof(MapMenu)) as MapMenu;
            if (Header != null) return;
            var b = new Border
            {
                Height = Width,
                Width = Height,
                Background = Background,
                CornerRadius = new CornerRadius(15)
            };

            var i = new Image() { Source = Image, Stretch = Stretch.Fill };
            b.Child = i;
            Header = b;
        }

        void MCenterButtonMouseEnter(object sender, MouseEventArgs e)
        {
            var dt = new DispatcherTimer(DispatcherPriority.Background) { Interval = new TimeSpan(0, 0, 0, 0, 500) };
            dt.Tick += (es, s) =>
            {
                if (null != sender && this.IsMouseOver)
                {
                    Expand();
                    SetLastTouch();
                    //sender.RaiseEvent(new RoutedEventArgs(MouseHoverEvent, this));
                }
                dt.Stop();
            };
            dt.Start();
        }

        private void SetLastTouch()
        {
            Expand();
            if (timeoutTimer == null || !timeoutTimer.IsEnabled)
            {
                timeoutTimer = new DispatcherTimer(DispatcherPriority.Background);
                timeoutTimer.Tick += TimeoutTimerTick;
                timeoutTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
                timeoutTimer.Start();
            }
            _lastTouch = DateTime.Now;
        }

        private void TimeoutTimerTick(object sender, EventArgs e)
        {
            if (_lastTouch.AddMilliseconds(3000) >= DateTime.Now) return;
            IsExpanded = false;
            timeoutTimer.Stop();
        }

        private void MCenterButtonLostMouseCapture(object sender, MouseEventArgs e)
        {
            if (RelativeElement != null && !isTouch)
            {
                var p = e.GetPosition(RelativeElement);
                if (mouseTime.AddMilliseconds(200) > DateTime.Now)
                {
                    if (IconTapped != null) IconTapped(this, new IconMovedEventArgs { Position = p });
                    //MapMenuCommands.ToggleExpansion.Execute(null, this);
                }
                if (IconReleased != null) IconReleased(this, new IconMovedEventArgs { Position = p });
                if (Moving)
                {
                    Moving = false;
                    if (IconMoveCompleted != null) IconMoveCompleted(this, new IconMovedEventArgs() { Position = p });
                }
            }

            if (mouseTime.AddMilliseconds(200) > DateTime.Now)
            {
                RaiseTapEvent();
                //MapMenuCommands.ToggleExpansion.Execute(null, this);
            }
            e.Handled = true;
        }

        private void MCenterButtonLostTouchCapture(object sender, TouchEventArgs e)
        {
            e.TouchDevice.Updated -= TouchDeviceUpdated;
            var p = e.GetTouchPoint(RelativeElement).Position;
            if (touchTime.AddMilliseconds(200) > DateTime.Now)
            {
                if (RelativeElement != null && IconTapped != null) IconTapped(this, new IconMovedEventArgs { Position = p });
                RaiseTapEvent();

                //MapMenuCommands.ToggleExpansion.Execute(null, this);
            }
            if (IconReleased != null) IconReleased(this, new IconMovedEventArgs { Position = p });
            if (Moving)
            {
                Moving = false;
                if (IconMoveCompleted != null) IconMoveCompleted(this, new IconMovedEventArgs() { Position = p });
            }
            isTouch = false;
        }

        private void MCenterButtonPreviewTouchMove(object sender, TouchEventArgs e)
        {
            SetLastTouch();
            if (RelativeElement != null && touchTime.AddMilliseconds(200) < DateTime.Now)
            {
                var p = e.GetTouchPoint(RelativeElement).Position;
                if (!Moving)
                {
                    Moving = true;
                    if (IconStartMoving != null) IconStartMoving(this, new IconMovedEventArgs() { Position = p });
                }
                if (IconMoved != null) IconMoved(this, new IconMovedEventArgs { Position = p });

            }
            e.Handled = true;
        }

        private void MCenterButtonPreviewTouchDown(object sender, TouchEventArgs e)
        {
            isTouch = true;
            SetLastTouch();
            touchTime = DateTime.Now;
            _mCenterButton.CaptureTouch(e.TouchDevice);
            e.TouchDevice.Updated += TouchDeviceUpdated;
            e.Handled = true;
        }

        private void TouchDeviceUpdated(object sender, EventArgs e)
        {
            SetLastTouch();
            var td = (TouchDevice)sender;
            if (RelativeElement == null || touchTime.AddMilliseconds(200) >= DateTime.Now) return;
            var p = td.GetTouchPoint(RelativeElement).Position;
            if (!Moving)
            {
                Moving = true;
                if (IconStartMoving != null) IconStartMoving(this, new IconMovedEventArgs() { Position = p });
            }
            if (IconMoved != null) IconMoved(this, new IconMovedEventArgs { Position = p });
        }

        private void MCenterButtonPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || isTouch) return;
            SetLastTouch();
            if (RelativeElement == null || mouseTime.AddMilliseconds(200) >= DateTime.Now) return;
            var p = e.GetPosition(RelativeElement);
            if (!Moving)
            {
                Moving = true;
                if (IconStartMoving != null) IconStartMoving(this, new IconMovedEventArgs() { Position = p });
            }
            if (IconMoved != null) IconMoved(this, new IconMovedEventArgs { Position = p });
        }

        private void MCenterButtonPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            SetLastTouch();
            //e.Handled = false;
        }

        private void MCenterButtonPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //if (_mm != null && _mm.MenuEnabled && !isTouch)
            if (!isTouch)
            {
                SetLastTouch();
                MapMenuCommands.Expand.Execute(null, this);
                mouseTime = DateTime.Now;
                _mCenterButton.CaptureMouse();
            }
            e.Handled = true;
        }

        private void mPanel_ChildArranged(object sender, UIElement child, double angle)
        {
            var childItem = child as MapMenuItem;

            if (childItem == null || childItem._mPanel == null) return;
            childItem._mPanel.AngleSpacing = Double.NaN;

            // Good if only one submenu is open at a time
            // childItem.mPanel.StartAngle = angle - 67.75;
            // childItem.mPanel.EndAngle = angle + 67.75;

            // Good if multiple submenus are open at a time
            childItem._mPanel.StartAngle = angle - 45.0;
            childItem._mPanel.EndAngle = angle + 45.00;
        }


        private void MCenterButtonClick(object sender, RoutedEventArgs e)
        {

            if (_mPanel.Children.Count > 0)
            {
                //MapMenuCommands.ToggleExpansion.Execute(null, this);
            }
            else
            {
                //RaiseTapEvent();
            }
            e.Handled = true;
        }

        public Point AlignReferencePoint
        {
            get
            {
                if (null != _mAlignPanel)
                {
                    return _mAlignPanel.AlignReferencePoint;
                }
                return new Point();
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            try
            {
                var infSize = new Size(Double.PositiveInfinity, Double.PositiveInfinity);

                _mCenterButton.Measure(infSize);

                _mAlignPanel.AlignReferencePoint = new Point(_mCenterButton.DesiredSize.Width * 0.5,
                                                            _mCenterButton.DesiredSize.Height * 0.5);

                _mPanel.Measure(infSize);
                _mAlignPanel.Measure(infSize);
            }
            catch (Exception)
            {

                //throw;
            }


            return base.MeasureOverride(constraint);

        }


        private void ToggleExpand()
        {
            IsExpanded = !IsExpanded;
            // TODO: If a given child element has already expanded children, we don't want to dial down the opacity even further!

            // Make sure we have the correct options for PART_Panel
            if (null != _mPanel)
            {
                // The really really nice way is to check neighbour arragements and clip in a way such that we don't draw over it
                if (mm.Radius != 0.0)
                {
                    _mPanel.Radius = mm.Radius;
                }
                else
                {
                    _mPanel.Radius = Math.Max(_mCenterButton.DesiredSize.Height, _mCenterButton.DesiredSize.Width) * 1.50;
                }

            }


            InvalidateTreeMeasure();
        }

        private void Expand()
        {
            if (mm != null && mm.MenuEnabled)
            {
                IsExpanded = true;
                // TODO: If a given child element has already expanded children, we don't want to dial down the opacity even further!

                // Make sure we have the correct options for PART_Panel
                if (null != _mPanel)
                {
                    // The really really nice way is to check neighbour arragements and clip in a way such that we don't draw over it


                    if (mm.Radius != 0.0)
                    {
                        _mPanel.Radius = mm.Radius;
                    }
                    else
                    {
                        _mPanel.Radius = Math.Max(_mCenterButton.DesiredSize.Height, _mCenterButton.DesiredSize.Width) * 1.50;
                    }
                }


                InvalidateTreeMeasure();
            }
        }

        private void InvalidateTreeMeasure()
        {
            InvalidateMeasure();
            _mPanel.InvalidateMeasure();
            _mAlignPanel.InvalidateMeasure();

            if (Parent is MapMenuItem)
            {
                ((MapMenuItem)Parent).InvalidateTreeMeasure();
            }
        }

        private bool CanToggleExpand()
        {
            if (null != Items && Items.Count > 0)
            {
                return true;
            }

            return false;
        }

        // Executed event handler.
        private static void ToggleCommandHandler(object target, ExecutedRoutedEventArgs e)
        {
            if (null != target && target is MapMenuItem)
            {
                ((MapMenuItem)target).ToggleExpand();
            }
        }

        // CanExecute event handler.
        private static void CanToggleHandler(object target, CanExecuteRoutedEventArgs e)
        {
            if (null != target && target is MapMenuItem)
            {
                e.CanExecute = ((MapMenuItem)target).CanToggleExpand();
            }
        }




    }

    public delegate void IconMovedEventHandler(object sender, IconMovedEventArgs e);

    public delegate void IconTapedEventHandler(object sender, IconMovedEventArgs e);

    public class IconMovedEventArgs
    {
        public Point Position { get; set; }
    }
}