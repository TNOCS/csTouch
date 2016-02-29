using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using BaseWPFHelpers;
using Caliburn.Micro;
using csShared;
using Microsoft.Surface.Presentation.Controls;
using Point = System.Windows.Point;

namespace csCommon.Plugins.DashboardPlugin
{
    public class DashboardControl : Control
    {
        private Grid PART_Content;

        static DashboardControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DashboardControl),
                new FrameworkPropertyMetadata(typeof(DashboardControl)));
        }

        private ScatterViewItem svi;

        public DashboardItem Item
        {
            get { return (DashboardItem)GetValue(ItemProperty); }
            set
            {
                SetValue(ItemProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for Item.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemProperty =
            DependencyProperty.Register("Item", typeof(DashboardItem), typeof(DashboardControl), new PropertyMetadata(null));

        private Grid PART_Control;
        private SurfaceButton sbSettings;
        private SurfaceButton sbResize;
        //private ScatterViewItem _svi;

        public override void OnApplyTemplate()
        {
            if (Execute.InDesignMode) return;
            //csCommon.Resources.FloatingStyles fs = new FloatingStyles();
            //var ct = fs.FindName("SimpleFloatingStyle") as ControlTemplate;
            Loaded += DashboardControl_Loaded;
            base.OnApplyTemplate();
            InitScatterViewEvents();
            Item.PropertyChanged += Item_PropertyChanged;
            //_svi = (ScatterViewItem)Helpers.FindElementOfTypeUp(this, typeof(ScatterViewItem));

            UpdateGridPosition();
            PART_Content = (Grid) GetTemplateChild("PART_Content");
            PART_Control = (Grid) GetTemplateChild("PART_Control");
            sbSettings = (SurfaceButton) GetTemplateChild("sbSettings");
            sbResize   = (SurfaceButton) GetTemplateChild("sbResize");
            sbSettings.Click += sbSettings_Click;
            //InitResize();
            if (PART_Content == null) return;
            
            var cc = new ContentControl();
            if (Item != null && Item.ViewModel != null)
            {
                var b = ViewLocator.LocateForModel(Item.ViewModel, null, null) as FrameworkElement;
                if (b == null) return;
                b.HorizontalAlignment = HorizontalAlignment.Stretch;
                b.VerticalAlignment = VerticalAlignment.Stretch;
                ViewModelBinder.Bind(Item.ViewModel, b, null);
                cc.Content = b;
            }
            PART_Content.Children.Add(cc);
        }

        void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "GridWidth" || e.PropertyName == "GridHeight")
            {
                UpdateGridPosition();
            }
        }

        void sbSettings_Click(object sender, RoutedEventArgs e)
        {
            AppStateSettings.Instance.Dashboards.ActiveDashboardItem = Item;
            AppStateSettings.Instance.TriggerScriptCommand(this,"FocusActiveDashboardItem");
            AppStateSettings.Instance.ActivateStartPanelTabItem("Dashboards");
        }

        //void InitResize()
        //{
        //    var start = new Point();

        //    sbResize.PreviewMouseDown += (e, s) =>
        //    {
        //        start = s.GetPosition(svi);
        //        s.Handled = true;
        //        s.MouseDevice.Capture(sbResize);
        //    };
        //    sbResize.PreviewMouseMove += (e, s) =>
        //    {
        //        if (s.LeftButton != MouseButtonState.Pressed) return;
        //        var p = s.GetPosition(svi);
        //        var difx = p.X - start.X;
        //        var dify = p.Y - start.Y;
        //        var nw = svi.ActualWidth + difx;
        //        svi.Width = nw;
        //        svi.Height = svi.ActualHeight + dify + 1;
        //        start = p;
        //        s.Handled = true;
        //    };
        //    sbResize.PreviewMouseUp += (e, s) => sbResize.ReleaseMouseCapture();

        //    sbResize.PreviewTouchDown += (e, s) =>
        //    {
        //        start = s.GetTouchPoint(svi).Position;
        //        s.Handled = true;
        //        s.TouchDevice.Capture(sbResize);
        //        AppStateSettings.Instance.Dashboards.ActiveDashboardItem = Item;
        //    };

        //    sbResize.PreviewTouchMove += (e, s) =>
        //    {
        //        var p = s.GetTouchPoint(svi).Position;
        //        var difx = p.X - start.X;
        //        var dify = p.Y - start.Y;
        //        var nw = svi.ActualWidth + difx;

        //        svi.Height = svi.ActualHeight + dify;
        //        svi.Width = nw;
        //        start = p;
        //        s.Handled = true;
        //    };
        //}

        //void SbResizePreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        //{
        //    sbResize.CaptureMouse();
        //}

        //void SbResizeTouchDown(object sender, System.Windows.Input.TouchEventArgs e)
        //{
            
        //}

        //void SbSettingsValueChanged(object sender, RoutedEventArgs e)
        //{
        //    if (!Item.Dashboard.CanEdit) return;
        //    //switch (sbSettings.Value.ToLower())
        //    //{
        //    //    case "bigger":
        //    //        Item.GridWidth += 2;
        //    //        Item.GridHeight += 2;
        //    //        Item.GridX += 1;
        //    //        Item.GridY += 1;
        //    //        break;
        //    //    case "smaller":
        //    //        Item.GridWidth -= 2;
        //    //        Item.GridHeight -= 2;
        //    //        Item.GridX -= 1;
        //    //        Item.GridY -= 1;
        //    //        break;
        //    //    case "wider":
        //    //        Item.GridWidth += 2;
                   
        //    //        Item.GridX += 1;
                    
        //    //        break;
        //    //    case "higher":
                    
        //    //        Item.GridHeight += 2;
                    
        //    //        Item.GridY += 1;
        //    //        break;
        //    //}
        //    UpdateGridPosition();
        //}

        void DashboardControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateGridPosition();
        }

        /// <summary>
        /// Sets the scatterviewitem at the right position and with the rights size 
        /// according to the grid position
        /// </summary>
        private void UpdateGridPosition()
        {
            Execute.OnUIThread(() =>
            {
                if (Item == null || Item.Dashboard == null) return;
                var fe = Application.Current.MainWindow;
                var posx = fe.ActualWidth/Item.Dashboard.GridWidth*Item.GridX;
                var posy = fe.ActualHeight/Item.Dashboard.GridHeight*Item.GridY;

                var width = fe.ActualWidth/Item.Dashboard.GridWidth*Item.GridWidth;
                var height = fe.ActualHeight/Item.Dashboard.GridHeight*Item.GridHeight;

                svi.Center = new Point(posx - width/2, posy - height/2);

                svi.CanRotate = false;
                svi.CanScale = false;
                svi.SetBinding(ScatterViewItem.CanMoveProperty,
                    new Binding {Source = Item.Dashboard, Path = new PropertyPath("CanEdit")});

                svi.SetBinding(WidthProperty, new Binding {Source = this, Path = new PropertyPath("Width")});

                svi.SetBinding(HeightProperty, new Binding {Source = this, Path = new PropertyPath("Height")});

                svi.Orientation = 0;
                //svi.UpdateLayout();
                //Width = width;
                //Height = height;
                if (ActualHeight == height && ActualWidth == width) return;
                BeginAnimation(WidthProperty, new DoubleAnimation(0, width, new Duration(TimeSpan.FromMilliseconds(100))));
                BeginAnimation(HeightProperty, new DoubleAnimation(0, height, new Duration(TimeSpan.FromMilliseconds(100))));
            });
        }

        /// <summary>
        /// Find hosting scatterviewitem and listen to events
        /// </summary>
        private void InitScatterViewEvents()
        {
            svi = (ScatterViewItem)Helpers.FindElementOfTypeUp(this, typeof(ScatterViewItem));
            svi.ContainerManipulationStarted += svi_ContainerManipulationStarted;
            svi.ContainerManipulationCompleted += svi_ContainerManipulationCompleted;
            svi.Loaded += svi_Loaded;
        }

        void svi_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateGridPosition();
        }

        private DispatcherTimer showTimer;
        private bool isMoving = false;

        void svi_ContainerManipulationStarted(object sender, ContainerManipulationStartedEventArgs e)
        {
            e.Handled = true;
            isMoving = true;
            PART_Control.Visibility = Visibility.Visible;
            AppStateSettings.Instance.Dashboards.ActiveDashboardItem = Item;
        }

        void svi_ContainerManipulationCompleted(object sender, ContainerManipulationCompletedEventArgs e)
        {
            var pos = svi.Center;
            pos.X += svi.ActualWidth/2;
            pos.Y += svi.ActualHeight/2;
            isMoving = false;
            var p = FindClosedGridPos(pos);
            if (p.X != Item.GridX || p.Y != Item.GridY)
            {
                Item.GridX = (int) p.X;
                Item.GridY = (int) p.Y;
            }
            UpdateGridPosition();
            showTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(5) };
            showTimer.Tick += (s, f) =>
            {
                if (!isMoving) PART_Control.Visibility = Visibility.Collapsed;
                showTimer.Stop();
            }
            ;
            showTimer.Start();
            if (Item != null && Item.Dashboard != null) Item.Dashboard.TriggerChanged();
        }

        /// <summary>
        /// Find grid position that is closed to given pos
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private Point FindClosedGridPos(Point pos)
        {
            var fe = Application.Current.MainWindow;
            var res = new Point
            {
                X = (int) Math.Round(Item.Dashboard.GridWidth*(pos.X/fe.ActualWidth)),
                Y = (int) Math.Round(Item.Dashboard.GridHeight*(pos.Y/fe.ActualHeight))
            };
            return res;
        }
    }
}
