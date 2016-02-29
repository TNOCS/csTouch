using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using csShared;
using csShared.Geo.Content;
using csShared.Utils;

namespace csGeoLayers.Content.RainRadar
{
    public class FlexibleRainRadarContent : PropertyChangedBase, IContent
    {
        private readonly Stack<string> requests = new Stack<string>();
        public Dictionary<string, string> Config = new Dictionary<string, string>();
        private string baseUrl;

        public bool IsOnline
        {
            get { return true; }
        }

        private double brLat;
        private double brLon;
        private bool busy;
        private GroupLayer gl;
        private Image i;
        private string imageUrl;
        private int interval = 5;
        private bool isRunning;
        private string lastImage = "";
        private Uri location;
        private int[] minutes = {0, 10, 20, 30, 40, 50};
        private string name;
        private double tlLat;
        private double tlLon;

        public static AppStateSettings AppState { get { return AppStateSettings.Instance; } }

        public Uri Location
        {
            get { return location; }
            set
            {
                location = value;
                NotifyOfPropertyChange(() => Location);
            }
        }

        public bool IsRunning
        {
            get { return isRunning; }
            set
            {
                isRunning = value;
                NotifyOfPropertyChange(() => IsRunning);
            }
        }

        public void Configure()
        {
        }

        public void Init()
        {
            foreach (var c in Config)
            {
                if (c.Key == "tlLon")
                    tlLon = Convert.ToDouble(c.Value, CultureInfo.InvariantCulture);
                if (c.Key == "tlLat")
                    tlLat = Convert.ToDouble(c.Value, CultureInfo.InvariantCulture);
                if (c.Key == "brLon")
                    brLon = Convert.ToDouble(c.Value, CultureInfo.InvariantCulture);
                if (c.Key == "brLat")
                    brLat = Convert.ToDouble(c.Value, CultureInfo.InvariantCulture);
                if (c.Key == "baseUrl")
                    baseUrl = c.Value;
                if (c.Key != "interval") continue;
                interval = Convert.ToInt32(c.Value);
                if (interval <= 0) continue;
                var idx = 60/interval;
                minutes = new int[idx];
                var count = 0;
                for (var i1 = 0; i1 < 60; i1 += interval)
                {
                    minutes[count] = i1;
                    count++;
                }
            }
        }

        //public string BaseUrl = "http://cool2.sensorlab.tno.nl:8000/BuienRadarService/RainImage/eu/warped/";
        //public string BaseUrl = "http://cool2.sensorlab.tno.nl:8000/BuienRadarService/RainImage/nl/warped/";
        //public string BaseUrl = "http://localhost:8000/BuienRadarService/RainImage/nl/warped/";

        public void Add()
        {
            Init();
            lastImage = "";
            IsRunning = true;
            gl = AppState.ViewDef.FindOrCreateGroupLayer(@"Weather/Rain");
            var w = new WebMercator();
            //Buienradar
            var wi = new RainRadarLayer {ID = Name};
            i = new Image {
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            UpdateImage(AppState.TimelineManager.FocusTime);
            var mpa = new MapPoint(tlLat, tlLon);
            var mpb = new MapPoint(brLat, brLon);
          
            mpa = w.FromGeographic(mpa) as MapPoint;
            mpb = w.FromGeographic(mpb) as MapPoint;
            var envelope = new Envelope(mpa, mpb);
            ElementLayer.SetEnvelope(i, envelope);

            wi.Children.Add(i);
            wi.Initialize();
            wi.Visible = true;
            gl.ChildLayers.Add(wi);

            AppState.TimelineManager.TimeChanged += TimelineManager_TimeChanged;

            AppState.CreateCache += AppState_CreateCache;
        }

        public void Remove()
        {
            IsRunning = false;
            AppState.ViewDef.RemoveLayer(@"Weather/Rain");
            AppState.CreateCache -= AppState_CreateCache;            
        }

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                NotifyOfPropertyChange(() => Name);
            }
        }

        public string ImageUrl
        {
            get { return imageUrl; }
            set
            {
                imageUrl = value;
                NotifyOfPropertyChange(() => ImageUrl);
            }
        }

        public string Folder { get; set; }

        private string GetImage(DateTime dt)
        {
            var m = 0;
            foreach (var mi in minutes.Where(mi => dt.Minute > mi))
                m = mi;
            var d = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, m, 0);
            return baseUrl + d.Year + "-" + d.Month + "-" + d.Day + " " + d.Hour + ":" + d.Minute;
        }

        private void AppState_CreateCache(object sender, EventArgs e) {
            ThreadPool.QueueUserWorkItem(delegate {
                var d = AppState.TimelineManager.Start;
                while (d < AppState.TimelineManager.End) {
                    try {
                        var a = new Uri(GetImage(d));
                        d = d.AddMinutes(interval);
                        var dc = new DownloadCache();
                        if (dc.ExistsLocal(a)) continue;
                        Console.WriteLine(@"Downloading: " + a.AbsolutePath);
                        dc.DownloadFile(a);
                        Thread.Sleep(250);
                    }
                    catch (Exception ex) {
                        Logger.Log("FlexibleRadarContent", ex.Message, "Error creating cache", Logger.Level.Error, true);
                    }
                }
            });
        }

        private void TimelineManager_TimeChanged(object sender, EventArgs e)
        {
            UpdateImage(AppState.TimelineManager.FocusTime);
        }

        private static BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }


        private void DownloadNext()
        {
            if (requests.Count <= 0) return;
            var im = requests.Peek();
            var a = new Uri(im);

            var dc = new DownloadCache();

            if (dc.ExistsLocal(a))
            {
                dc.DownloadFileCompleted += (e, f) =>
                {
                    if (requests.Any()) requests.Pop();
                    Execute.OnUIThread(() =>
                    {
                        if ((f.Bytes.Count() <= 0)) return;
                        i.Source = LoadImage(f.Bytes);
                        i.Opacity = 1;
                    });
                };

                dc.DownloadFile(a);
            }
            else
            {
                if (busy) return;
                Console.WriteLine(@"Starting download: " + a.AbsoluteUri);

                busy = true;
                dc.DownloadFileCompleted += (e, f) =>
                {
                    busy = false;
                    if (requests.Count > 0) requests.Pop();
                    Execute.OnUIThread(() =>
                    {
                        if ((f.Bytes != null && f.Bytes.Count() > 0) &&
                            GetImage(AppState.TimelineManager.FocusTime) == im)
                        {
                            i.Source = LoadImage(f.Bytes);
                            i.Opacity = 1;
                        }
                    });
                    DownloadNext();
                };

                dc.DownloadFile(a);
                if (GetImage(AppState.TimelineManager.FocusTime) == im)
                {
                    i.Opacity = 0.5;
                }
            }
        }

        private void UpdateImage(DateTime date)
        {
            var im = GetImage(date);
            if (im == lastImage || requests.Contains(im)) return;
            lastImage = im;
            requests.Push(im);
            DownloadNext();
            i.Opacity = 0.5;
        }
    }
}