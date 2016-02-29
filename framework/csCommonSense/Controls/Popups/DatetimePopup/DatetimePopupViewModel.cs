using Caliburn.Micro;
using csShared.Interfaces;
using System;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace csShared.Controls.Popups.DatetimePopup
{
    

    [Export(typeof(IPopupScreen))]
    public class DatetimePopupViewModel : Screen, IPopupScreen
    {
        private bool autoClose = true;
        private Brush background = Brushes.White;
        private Brush border = Brushes.Black;
        private string displayProperty;

        private BindableCollection<System.Windows.Controls.MenuItem> items =
            new BindableCollection<System.Windows.Controls.MenuItem>();

        private BindableCollection<object> objects = new BindableCollection<object>();
        private Point point;
        private FrameworkElement relativeElement;
        private Point relativePosition;
        private TimeSpan? timeOut;
        private VerticalAlignment verticalAlignment;
        private DispatcherTimer toTimer;

        private DatetimePopupView view;

        public DatetimePopupViewModel()
        {
           
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
                relativeElement = value;
                UpdatePosition();

                RelativeElement.LayoutUpdated += RelativeElement_LayoutUpdated;
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


      
        public void Close()
        {
            AppState.Popups.Remove(this);
        }

        private void RelativeElement_LayoutUpdated(object sender, EventArgs e)
        {
            UpdatePosition();
        }

        protected override void OnViewLoaded(object theView)
        {
            base.OnViewLoaded(theView);
            view = (DatetimePopupView)theView;

            UpdatePosition();

            //Items = new BindableCollection<System.Windows.Controls.MenuItem>();

            if (TimeOut.HasValue)
            {
                toTimer = new DispatcherTimer();
                toTimer.Interval = TimeOut.Value;
                toTimer.Tick += toTimer_Tick;
                toTimer.Start();
            }
        }

        private void toTimer_Tick(object sender, EventArgs e)
        {
            toTimer.Stop();
            Close();
        }

        private void UpdatePosition()
        {
            if (view == null) return;
            if (relativeElement != null)
            {
                Point = RelativeElement.TranslatePoint(RelativePosition, Application.Current.MainWindow);
            }

            view.VerticalAlignment = VerticalAlignment;

            view.touchDatePicker.Margin = new Thickness(Point.X, Point.Y, 0, 0);

            //switch (view.VerticalAlignment)
            //{
            //    case VerticalAlignment.Top:
            //        view.Items.Margin = new Thickness(Point.X, Point.Y, 0, 0);
            //        break;
            //    case VerticalAlignment.Bottom:
            //        view.Items.Margin = new Thickness(Point.X, 0, 0,
            //            Application.Current.MainWindow.ActualHeight - Point.Y);
            //        //view.Items.Margin = new Thickness(Point.X, Point.Y, 0, 0);
            //        break;
            //}
        }
    }
}