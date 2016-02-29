using System;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using csShared;
using csShared.Geo;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Threading;

namespace csGeoLayers.Content.RainRadar
{
    [Serializable]
    public class RainRadarLayer : SettingsElementLayer, INotifyPropertyChanged
    {

        public new event PropertyChangedEventHandler PropertyChanged; // FIXME TODO "new" keyword missing?

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }


        private readonly WebMercator _mercator = new WebMercator();
        //public event EventHandler Loaded;

        private DispatcherTimer RainRadarUpdate;


        //private string _url = "http://openflights.svn.sourceforge.net/viewvc/openflights/openflights/data/airports.dat";

        public override void Initialize()
        {
            //ID = "Rain Radar Europe";
            base.Initialize();
            SpatialReference = new SpatialReference(4326);
            RainRadarUpdate = new DispatcherTimer();
            RainRadarUpdate.Interval = new TimeSpan(0, 1, 0);
            RainRadarUpdate.Tick += RainRadarUpdate_Tick;
        }

        //private Guid _id;

        private bool _isrefreshing= false;
        public bool IsRefreshing { get { return _isrefreshing; } set { _isrefreshing = value; NotifyPropertyChanged("IsRefreshing"); } }

        private bool _notifications = false;
        public bool Notifications
        {
            get { return _notifications; }
            set { _notifications = value; NotifyPropertyChanged("Notifications"); }
        }

        public void Start()
        {
            RainRadarUpdate.Start();
            IsRefreshing = true;
        }

        public void Stop()
        {
            RainRadarUpdate.Stop();
            IsRefreshing = false;
        }

        public void SetInterval(int interval)
        {
            RainRadarUpdate.Interval = new TimeSpan(0,interval,0);
        }

        void RainRadarUpdate_Tick(object sender, EventArgs e)
        {

            WebMercator w = new WebMercator();
            //WebClient wc = new WebClient();
            var topleft = new KmlPoint(-14.9515, 59.9934);
            var bottomright = new KmlPoint(20.4106, 41.4389);
            var fname = "http://www2.buienradar.nl/euradar/latlon_0.gif";//"rainradar.gif";
//            File.Delete(fname);
//            wc.DownloadFile("http://www2.buienradar.nl/euradar/latlon_0.gif", fname);

//            var gwrap = new GdalWrapper();
//            var f = gwrap.WarpImage(fname, topleft.Latitude, topleft.Longitude, bottomright.Latitude, bottomright.Longitude, 4326, 3857, 5000);
//            fname = Directory.GetCurrentDirectory() + "\\" + f;
            //var f = WarpImage(fname, topleft, bottomright, 4326, 3857, 5000);
            var i = new Image
            {
                //Source = new BitmapImage(new Uri("file://" + fname)),
                Source = new BitmapImage(new Uri(fname)),
                IsHitTestVisible = false,
                Stretch = Stretch.Fill
            };

            //<LatLonBox><north>59.9934</north><south>41.4389</south><east>20.4106</east><west>-14.9515</west></LatLonBox>
            var mpa = new MapPoint(topleft.Longitude, topleft.Latitude);
            var mpb = new MapPoint(bottomright.Longitude, bottomright.Latitude);
            mpa = w.FromGeographic(mpa) as MapPoint;
            mpb = w.FromGeographic(mpb) as MapPoint;
            var envelope = new Envelope(mpa, mpb);
            ElementLayer.SetEnvelope(i, envelope);
            this.Children.Clear();
            this.Children.Add(i);

            if (Notifications)
            {
                AppStateSettings.Instance.TriggerNotification(new NotificationEventArgs()
                {
                    Duration = new TimeSpan(0, 0, 0, 3),
                    Text = "Updated Rain Radar Image at"+ ":" + DateTime.Now
                });

            }
        }

       
        public override void StartSettings()
        {
            var fe = new FloatingElement
            {
                OpacityDragging = 0.5,
                OpacityNormal = 1.0,
                CanMove = true,
                CanRotate = true,
                CanScale = true,
                StartOrientation = 0,
                Background = Brushes.DarkOrange,
                StartPosition = new Point(400,400),
                StartSize = new Size(400, 500),
                ShowsActivationEffects = false,
                RemoveOnEdge = true,
                Contained = true,
                Title = "Rain Radar Configuration",
                Foreground = Brushes.White,
                DockingStyle = DockingStyles.None,
                ModelInstance = new RainRadarConfigViewModel(this)
            };
            AppStateSettings.Instance.FloatingItems.Add(fe);

        }
    }
}