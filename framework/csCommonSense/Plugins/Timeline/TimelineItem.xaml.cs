using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using csShared.Interfaces;
using csShared;
using csShared.Geo;

namespace csCommon.Plugins.Timeline
{
    /// <summary>
    /// Interaction logic for TimelineItem.xaml
    /// </summary>
    // TODO This code looks a lot like the code in AggregateTimelineView.xaml.cs. Are they the same (EV)?
    public partial class TimelineItem : UserControl
    {
        private string text;

        public string Text
        {
            get { return text; }
            set
            {
                text = value;
                tbEventName.Text = text;
            }
        }

        public bool HasFocus { get; set; }

        public Color Color { get; set; }

        public DateTime ItemDateTime { get; set; }

        public TimeSpan ItemRange { get; set; }

        public double TlLength { get; set; }

        private readonly ITimelineManager tlManager;

        public TimelineView TlView { get; set; }

        public Guid Id { get; set; }


        public TimelineItem(ITimelineManager tlManager)
        {
            InitializeComponent();
            this.tlManager = tlManager;
            this.tlManager.TimeChanged += _tlManager_TimeChanged;
        }

        public KmlPoint EventPoint { get; set; }

        public void _tlManager_TimeChanged(object sender, EventArgs e)
        {
            if (TimelineView.TimelineViewInstance == null) return;
            if (ItemDateTime >= tlManager.Start && ItemDateTime <= tlManager.End)
            {
                Visibility = Visibility.Visible;
                //var ts = tlManager.End - tlManager.Start;
                //var resX = TimelineView.TimelineViewInstance.ActualWidth/ts.TotalSeconds;
                //var tsC = ItemDateTime - tlManager.Start;
                
                //var itemX = tsC.TotalSeconds * resX;
                //this.Margin = new Thickness(itemX - 12.5,40,0,0);
            }
            else
            {
                Visibility = Visibility.Collapsed;
            }
        }

        private void MouseFocusOnEvent(object sender, MouseButtonEventArgs e)
        {
            if (EventPoint == null) return;
            AppStateSettings.GetInstance().ViewDef.ZoomTo(EventPoint, 3000);
            if (ItemRange == new TimeSpan()) return;
            tlManager.Start = ItemDateTime.Add(-ItemRange);
            tlManager.End = ItemDateTime.Add(ItemRange);
            tlManager.ForceTimeContentChanged();
        }
    }
}
