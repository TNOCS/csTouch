using Caliburn.Micro;
using csShared.Interfaces;
using System;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using OxyPlot;
using VerticalAlignment = System.Windows.VerticalAlignment;

namespace csShared.Controls.Popups.MenuPopup
{
    public class MenuSelectedEventArgs : EventArgs
    {
        public object Object { get; set; }
    }

    [Export(typeof(IPopupScreen))]
    public class MenuPopupViewModel : Screen, IPopupScreen
    {
        private bool   autoClose  = true;
        private Brush  background = Brushes.White;
        private Brush  border     = Brushes.Black;
        private string displayProperty;

        private BindableCollection<System.Windows.Controls.MenuItem> items = new BindableCollection<System.Windows.Controls.MenuItem>();

        private BindableCollection<object> objects = new BindableCollection<object>();
        private Point                      point;
        private FrameworkElement           relativeElement;
        private Point                      relativePosition;
        private TimeSpan?                  timeOut;
        private VerticalAlignment          verticalAlignment;
        private DispatcherTimer            toTimer;

        private MenuPopupView view;

        public MenuPopupViewModel()
        {
            Objects.CollectionChanged += Items_CollectionChanged;
        }

        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public bool AutoClose
        {
            get { return autoClose; }
            set { autoClose = value; }
        }

        public VerticalAlignment VerticalAlignment
        {
            get { return verticalAlignment; }
            set
            {
                verticalAlignment = value;
                UpdatePosition();
                NotifyOfPropertyChange(() => VerticalAlignment);
            }
        }

        public Point Point
        {
            get { return point; }
            set
            {
                point = value;
                NotifyOfPropertyChange(() => Point);
            }
        }

        public string DisplayProperty
        {
            get { return displayProperty; }
            set
            {
                displayProperty = value;
                NotifyOfPropertyChange(() => DisplayProperty);
            }
        }

        public BindableCollection<object> Objects
        {
            get { return objects; }
            set
            {
                objects = value;
                NotifyOfPropertyChange(() => Objects);
            }
        }

        public BindableCollection<System.Windows.Controls.MenuItem> Items
        {
            get { return items; }
            set
            {
                items = value;
                NotifyOfPropertyChange(() => Items);
            }
        }

        public Brush Background
        {
            get { return background; }
            set
            {
                background = value;
                NotifyOfPropertyChange(() => Background);
            }
        }

        public FrameworkElement RelativeElement
        {
            get { return relativeElement; }
            set
            {
                if (value == null)
                {
                    // Parent closed, so close too.
                    Close();
                    return;
                }
                relativeElement = value; 
                UpdatePosition();
                relativeElement.LayoutUpdated += RelativeElement_LayoutUpdated;
                NotifyOfPropertyChange(() => RelativeElement);
            }
        }

        public TimeSpan? TimeOut
        {
            get { return timeOut; }
            set { timeOut = value; }
        }

        public Point RelativePosition
        {
            get { return relativePosition; }
            set
            {
                relativePosition = value;
                UpdatePosition();
                NotifyOfPropertyChange(() => RelativePosition);
            }
        }

        public Brush Border
        {
            get { return border; }
            set
            {
                border = value;
                NotifyOfPropertyChange(() => Border);
            }
        }

        public event EventHandler<MenuSelectedEventArgs> Selected;


        public System.Windows.Controls.MenuItem AddMenuItem(string header)
        {
            var miRemove = MenuHelpers.CreateMenuItem(header);
            miRemove.Click += miRemove_Click;
            Items.Add(miRemove);
            return miRemove;
        }

        public void AddMenuItems(string[] headers)
        {
            foreach (var header in headers)
            {
                var miRemove = new System.Windows.Controls.MenuItem
                {
                    Header     = header,
                    Tag        = header,
                    FontFamily = new FontFamily("Segoe360"),
                    FontSize   = 20
                };
                miRemove.Click += miRemove_Click;
                Items.Add(miRemove);
            }
        }

        private void miRemove_Click(object sender, RoutedEventArgs e)
        {
            if (AutoClose) Close();
            if (Selected == null) return;
            var obj = ((System.Windows.Controls.MenuItem)sender).Tag;
            Selected(this, new MenuSelectedEventArgs { Object = obj });
        }

        private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add) return;
            foreach (var a in e.NewItems)
            {
                var mi = new System.Windows.Controls.MenuItem { Tag = a };

                if (!string.IsNullOrEmpty(DisplayProperty))
                {
                    var displayInfo = a.GetType().GetProperty(DisplayProperty);
                    mi.Header = displayInfo.GetValue(a, null);
                }
                else
                {
                    mi.Header = a.ToString();
                }
                mi.FontSize = 20;
                mi.FontFamily = new FontFamily("Segoe360");

                Items.Add(mi);
                mi.Click += MiClick;
                mi.TouchDown += MiClick;
            }
        }

        private void MiClick(object sender, RoutedEventArgs e)
        {
            if (AutoClose) Close();
            if (Selected != null)
                Selected(this, new MenuSelectedEventArgs { Object = ((System.Windows.Controls.MenuItem)sender).Tag });
        }

        public void Close()
        {
            if (toTimer != null) toTimer.Stop();
            if (relativeElement != null) relativeElement.LayoutUpdated -= RelativeElement_LayoutUpdated;
            AppState.Popups.Remove(this);
        }

        private void RelativeElement_LayoutUpdated(object sender, EventArgs e)
        {
            UpdatePosition();
        }

        protected override void OnViewLoaded(object theView)
        {
            base.OnViewLoaded(theView);
            view = (MenuPopupView)theView;

            UpdatePosition();

            //Items = new BindableCollection<System.Windows.Controls.MenuItem>();

            if (!TimeOut.HasValue) return;
            toTimer = new DispatcherTimer { Interval = TimeOut.Value };
            toTimer.Tick += toTimer_Tick;
            toTimer.Start();
        }

        private void toTimer_Tick(object sender, EventArgs e)
        {
            toTimer.Stop();
            toTimer.Tick -= toTimer_Tick;
            Close();
        }

        private void UpdatePosition()
        {
            if (view == null) return;
            if (relativeElement != null)
            {
                Point = RelativeElement.TranslatePoint(RelativePosition, Application.Current.MainWindow);
                if (Point.X.IsZero() && Point.Y.IsZero()) {
                    Close();
                    return;
                }
            }

            view.VerticalAlignment = VerticalAlignment;

            switch (view.VerticalAlignment)
            {
                case VerticalAlignment.Top:
                    view.Items.Margin = new Thickness(Point.X, Point.Y, 0, 0);
                    break;
                case VerticalAlignment.Bottom:
                    view.Items.Margin = new Thickness(Point.X, 0, 0,
                        Application.Current.MainWindow.ActualHeight - Point.Y);
                    //view.Items.Margin = new Thickness(Point.X, Point.Y, 0, 0);
                    break;
            }
            if (toTimer == null) return;
            toTimer.Stop();
            toTimer.Start();
        }
    }
}