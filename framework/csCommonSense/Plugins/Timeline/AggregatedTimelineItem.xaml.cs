using System;
using System.Windows;
using System.Windows.Threading;
using csEvents;
using csShared.Interfaces;
using csShared;
using csShared.Geo;

namespace csCommon.Plugins.Timeline
{
    /// <summary>
    /// Interaction logic for TimelineItem.xaml
    /// </summary>
    // TODO This code looks a lot like the code in TimelineItem.cs. Are they the same (EV)?
    public partial class AggregatedTimelineItem
    {
        private TimelineItem item;

        public TimelineItem Item
        {
            get { return item; }
            set { 
                item = value;
                tbText.Text = value.Text;
            }
        }

        public string Text { get; set; }

        public bool HasFocus { get; set; }

        public DateTime ItemDateTime { get; set; }

        public TimeSpan ItemRange { get; set; }

        public double TlLength { get; set; }

        private ITimelineManager tlManager;

        public TimelineView TlView { get; set; }
        public DateTime LastMove { get; set; }

        public AggregatedTimelineItem()//ITimelineManager tlManager
        {
            InitializeComponent();
            Loaded += AggregatedTimelineItem_Loaded;
            bEvent.PreviewMouseMove += (e, f) => LastMove = DateTime.Now;
        }

        void AggregatedTimelineItem_Loaded(object sender, RoutedEventArgs e)
        {
            tlManager = AppStateSettings.GetInstance().TimelineManager;
            bDivider.BorderThickness = new Thickness(DividerWidth, 0, 0, 0);
        }

        public KmlPoint EventPoint { get; set; }
        readonly DispatcherTimer dt = new DispatcherTimer();

        public double DividerWidth { get; set; }

        internal void ShowEvent(TimelineItem ti)
        {
            Item = ti;
            LastMove = DateTime.Now;
            bEvent.Visibility = Visibility.Visible;
            dt.Stop();
        }

        internal void ShowEvent(IEvent ti)
        {
            Item = new TimelineItem(AppStateSettings.Instance.TimelineManager)
                {
                    EventPoint = new KmlPoint(ti.Longitude,ti.Latitude)
                };
            tbText.Text = ti.Name;
            LastMove = DateTime.Now;
            bEvent.Visibility = Visibility.Visible;
            dt.Stop();
        }

        internal void RemoveLater()
        {
            dt.Stop();
            LastMove = DateTime.Now;
            dt.Interval = new TimeSpan(0,0,0,3);
            dt.Tick += (e, s) =>
            {
                if (LastMove.AddSeconds(3) >= DateTime.Now) return;
                bEvent.Visibility = Visibility.Collapsed;
                dt.Stop();
            };
            dt.Start();
        }

        private void SurfaceButton_Click_1(object sender, System.Windows.RoutedEventArgs e)
        {
            if (item == null) return;
            if (Item.EventPoint != null) AppStateSettings.GetInstance().ViewDef.ZoomTo(Item.EventPoint, 3000);
            if (Item.ItemRange == new TimeSpan()) return;
            tlManager.Start = Item.ItemDateTime.Add(-Item.ItemRange);
            tlManager.End = Item.ItemDateTime.Add(Item.ItemRange);
            tlManager.ForceTimeContentChanged();
        }
    }
}
