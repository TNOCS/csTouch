using csShared.TabItems;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace csShared.Controls.SlideTab
{
    public enum TabOrientation
    {
        Horizontal,
        VerticalRight,
        Vertical
    }

    public enum TabHeaderStyle
    {
        Image,
        ImageText,
        Text
    }

    public class SlideTabItem : TabItem
    {
        public static readonly DependencyProperty ItemProperty =
            DependencyProperty.Register("Item", typeof(StartPanelTabItem), typeof(SlideTabItem),
                new UIPropertyMetadata(null));

        public static readonly DependencyProperty ContainerProperty =
            DependencyProperty.Register("Container", typeof(FrameworkElement), typeof(SlideTabItem),
                new UIPropertyMetadata(null));


        // Using a DependencyProperty as the backing store for TabText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TabTextProperty =
            DependencyProperty.Register("TabText", typeof(string), typeof(SlideTabItem), new PropertyMetadata(""));


        // Using a DependencyProperty as the backing store for Orientation. This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(TabOrientation), typeof(SlideTabItem),
                new PropertyMetadata(TabOrientation.Horizontal));

        public static readonly DependencyProperty SupportImageProperty =
            DependencyProperty.Register("SupportImage", typeof(ImageSource), typeof(SlideTabItem),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register("Image", typeof(ImageSource), typeof(SlideTabItem), new PropertyMetadata(null));

        public static readonly DependencyProperty ShowSupportImageProperty =
            DependencyProperty.Register("ShowSupportImage", typeof(Visibility), typeof(SlideTabItem),
                new PropertyMetadata(Visibility.Collapsed));

        public static readonly DependencyProperty ShowHeaderImageProperty =
            DependencyProperty.Register("ShowHeaderImage", typeof(Visibility), typeof(SlideTabItem),
                new PropertyMetadata(Visibility.Visible));

        public static readonly DependencyProperty ShowHeaderTextProperty =
            DependencyProperty.Register("ShowHeaderText", typeof(Visibility), typeof(SlideTabItem),
                new PropertyMetadata(Visibility.Visible));

        private bool lastSelected;
        private DateTime mouseStart;


        private bool captured;
        private FrameworkElement fe;
        private TabHeaderStyle headerStyle = TabHeaderStyle.Image;
        private double last;
        private Point mousePos;

        public string TabText
        {
            get { return (string)GetValue(TabTextProperty); }
            set { SetValue(TabTextProperty, value); }
        }

        public TabOrientation Orientation
        {
            get { return (TabOrientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }


        public StartPanelTabItem Item
        {
            get { return (StartPanelTabItem)GetValue(ItemProperty); }
            set { SetValue(ItemProperty, value); }
        }

        public FrameworkElement Container
        {
            get { return (FrameworkElement)GetValue(ContainerProperty); }
            set { SetValue(ContainerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Item. This enables animation, styling, binding, etc...

        public TabHeaderStyle HeaderStyle
        {
            get { return headerStyle; }
            set { headerStyle = value; }
        }


        public ImageSource SupportImage
        {
            get { return (ImageSource)GetValue(SupportImageProperty); }
            set { SetValue(SupportImageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SupportImage. This enables animation, styling, binding, etc...


        public ImageSource Image
        {
            get { return (ImageSource)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Image. This enables animation, styling, binding, etc...


        public Visibility ShowSupportImage
        {
            get { return (Visibility)GetValue(ShowSupportImageProperty); }
            set { SetValue(ShowSupportImageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowSupportImage. This enables animation, styling, binding, etc...


        public Visibility ShowHeaderImage
        {
            get { return (Visibility)GetValue(ShowHeaderImageProperty); }
            set { SetValue(ShowHeaderImageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowHeaderImage. This enables animation, styling, binding, etc...


        public Visibility ShowHeaderText
        {
            get { return (Visibility)GetValue(ShowHeaderTextProperty); }
            set { SetValue(ShowHeaderTextProperty, value); }
        }

        protected override void OnSelected(RoutedEventArgs e)
        {
            base.OnSelected(e);
        }

        // Using a DependencyProperty as the backing store for ShowHeaderText. This enables animation, styling, binding, etc...

        public event EventHandler Selected;
        public event EventHandler Deselected;


        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();


            if (Item != null)
            {
                Item.PropertyChanged -= Item_PropertyChanged;
                Header = Item.Name;
                Item.PropertyChanged += Item_PropertyChanged;
            }

            ShowHeaderImage = (HeaderStyle == TabHeaderStyle.Image || HeaderStyle == TabHeaderStyle.ImageText)
                ? Visibility.Visible
                : Visibility.Collapsed;

            ShowHeaderText = (HeaderStyle == TabHeaderStyle.Text || HeaderStyle == TabHeaderStyle.ImageText)
                ? Visibility.Visible
                : Visibility.Collapsed;

            ShowSupportImage = (SupportImage != null) ? Visibility.Visible : Visibility.Collapsed;

            fe = Template.FindName("Bd", this) as FrameworkElement;
            if (fe != null)
            {
                fe.IsManipulationEnabled = true;
                fe.ManipulationStarted += SlideTabItemManipulationStarted;
                fe.ManipulationStarting += SlideTabItemManipulationStarting;
                fe.ManipulationDelta += SlideTabItemManipulationDelta;


                fe.IsHitTestVisible = true;
                fe.TouchDown += SlideTabItemTouchDown;

                fe.PreviewMouseDown += SlideTabItemMouseDown;
                fe.MouseLeave += SlideTabItemMouseLeave;
                fe.PreviewMouseUp += SlideTabItemMouseUp;
                fe.MouseMove += SlideTabItemMouseMove;
            }
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Name")
            {
                Header = Item.Name;
            }
        }


        private void SlideTabItemMouseMove(object sender, MouseEventArgs e)
        {
            if (!captured || Container == null) return;
            if (Orientation == TabOrientation.Horizontal)
            {
                var delta = e.GetPosition(this).Y - mousePos.Y;
                var h = Math.Min(Math.Max(Container.MinHeight, Container.ActualHeight - delta),
                    Container.MaxHeight);

                Container.Height = h;
            }
            if (Orientation == TabOrientation.Vertical)
            {
                var delta = e.GetPosition(this).X - mousePos.X;
                var h = Math.Min(Math.Max(Container.MinWidth, Container.ActualWidth + delta),
                    Container.MaxWidth);

                Container.Width = h;
            }
            if (Orientation == TabOrientation.VerticalRight)
            {
                var delta = e.GetPosition(this).X - mousePos.X;

                var h = Math.Min(Math.Max(Container.MinWidth, Container.ActualWidth - delta),
                    Container.MaxWidth);

                Container.Width = h;
            }
        }

        private void SlideTabItemMouseUp(object sender, MouseButtonEventArgs e)
        {
            captured = false;
            fe.ReleaseMouseCapture();
            if (Container == null || mouseStart.AddMilliseconds(200) <= DateTime.Now) return;
            if (Orientation == TabOrientation.Horizontal)
            {
                if (Container.Height > Container.MaxHeight - 10 && (lastSelected == IsSelected))
                {
                    Container.Height = Container.MinHeight;
                }
                else if (Container.Height < Container.MinHeight + 10)
                {
                    Container.Height = Container.MaxHeight;
                }
            }
            if (Orientation == TabOrientation.Vertical)
            {
                if (Container.Width > Container.MaxWidth - 10 && (lastSelected == IsSelected))
                {
                    Container.Width = Container.MinWidth;
                }
                else
                {
                    Container.Width = Container.MaxWidth;
                }
            }
            if (Orientation != TabOrientation.VerticalRight) return;
            if (Container.Width > Container.MaxWidth - 10 && (lastSelected == IsSelected))
            {
                Container.Width = Container.MinWidth;
            }
            else
            {
                Container.Width = Container.MaxWidth;
            }
        }

        private void SlideTabItemMouseLeave(object sender, MouseEventArgs e)
        {
            captured = false;
        }

        private void SlideTabItemMouseDown(object sender, MouseButtonEventArgs e)
        {
            lastSelected = IsSelected;
            if (Container != null) captured = true;
            e.MouseDevice.Capture(fe);
            mouseStart = DateTime.Now;

            mousePos = e.GetPosition(this);
        }

        private void SlideTabItemTouchDown(object sender, TouchEventArgs e)
        {
            lastSelected = IsSelected;
            IsSelected = true;

            var dt = DateTime.Now;
            e.TouchDevice.Deactivated += (s, tea) =>
            {
                if (Container == null || dt.AddMilliseconds(200) <= DateTime.Now) return;
                if (Orientation == TabOrientation.Horizontal)
                {
                    if (Container.Height > Container.MaxHeight - 10 &&
                        lastSelected == IsSelected)
                    {
                        Container.Height = Container.MinHeight;
                    }
                    else if (Container.Height < Container.MinHeight + 10)
                    {
                        Container.Height = Container.MaxHeight;
                    }
                }
                else
                {
                    if (Container.Width > Container.MaxWidth - 10 &&
                        lastSelected == IsSelected)
                    {
                        Container.Width = Container.MinWidth;
                    }
                    else
                    {
                        Container.Width = Container.MaxWidth;
                    }
                }
            };
        }

        private void SlideTabItemManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (Container == null) return;
            if (Orientation == TabOrientation.Horizontal)
            {
                //todo: uitzoeken hoe dit komt
                if (e.DeltaManipulation.Translation.Y != -last)
                {
                    var h = Math.Min(Math.Max(Container.MinHeight, Container.ActualHeight - e.DeltaManipulation.Translation.Y), Container.MaxHeight);
                    last = e.DeltaManipulation.Translation.Y;
                    Container.Height = h;
                }
            }
            if (Orientation == TabOrientation.Vertical)
            {
                if (e.DeltaManipulation.Translation.X != -last)
                {
                    var h = Math.Min(Math.Max(Container.MinWidth, Container.ActualWidth + e.DeltaManipulation.Translation.X), Container.MaxWidth);
                    last = e.DeltaManipulation.Translation.X;
                    Container.Width = h;
                }
            }
            if (Orientation != TabOrientation.VerticalRight) return;
            if (e.DeltaManipulation.Translation.X != -last)
            {
                var h = Math.Min(Math.Max(Container.MinWidth, Container.ActualWidth - e.DeltaManipulation.Translation.X), Container.MaxWidth);
                last = e.DeltaManipulation.Translation.X;
                Container.Width = h;
            }
        }

        private void SlideTabItemManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = Container;
            e.Handled = true;
        }

        private void SlideTabItemManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            IsSelected = true;
        }

        internal void Select()
        {
            if (Selected != null) Selected(this, null);
        }

        internal void Deselect()
        {
            if (Deselected != null) Deselected(this, null);
        }
    }
}