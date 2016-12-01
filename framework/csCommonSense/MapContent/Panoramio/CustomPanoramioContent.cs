using Caliburn.Micro;
using csCommon.Logging;
using csCommon.Types.DataServer.PoI;
using csShared;
using DataServer;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Media;

namespace csCommon.MapContent.CustomPanoramio
{
    public class CustomPanoramioContent : PropertyChangedBase, csShared.Geo.Content.IContent
    {

        public static CustomPanoramioContent AddCustomPanoramioContent()
        {
            var content = new CustomPanoramioContent() { Name = "Purple Nectar Photo layer" };
            AppStateSettings.Instance.ViewDef.Content.Add(content);
            return content;
        }

        private string _imageUrl;

        private Uri _location;
        private string _name;
        private bool isRunning;

        public bool IsOnline
        {
            get { return true; }
        }


        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public Uri Location
        {
            get { return _location; }
            set
            {
                _location = value;
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
        }

        public SaveService PanoramioService { get; private set; }
        private PoI mImagePoiStyle;
        private WebClient mWebClient;
        public string Url { get; private set; }
        public int ReloadInMinutes { get; private set; }
        private System.Timers.Timer mTimer;

        public void Add()
        {

            PanoramioService = new SaveService()
            {
                IsLocal = true,
                Name = "PanoramioEx",
                Id = Guid.NewGuid(),
                IsFileBased = false,
                StaticService = true,
                IsVisible = false,
                RelativeFolder = "Social Media"

            };

            PanoramioService.Init(Mode.client, AppState.DataServer);
            // TODO Check met Arnoud of dit niet de folder moet zijn die in de configoffline staat?
            PanoramioService.Folder = Directory.GetCurrentDirectory() + @"\PoiLayers\Social Media";
            PanoramioService.InitPoiService();

            PanoramioService.Settings.OpenTab = true;
            PanoramioService.Settings.Icon = "brugwhite.png";
            PanoramioService.AutoStart = true;


            mImagePoiStyle = new PoI
            {
                PoiId = "Image",
                Style = new PoIStyle
                {
                    DrawingMode = DrawingModes.Image,
                    FillColor = Colors.Red,
                    CallOutFillColor = Colors.White,
                    CallOutForeground = Colors.Black,
                    IconWidth = 30,
                    IconHeight = 30,
                    TapMode = TapMode.OpenMedia,
                    TitleMode = TitleModes.None
                },
                MetaInfo = new MetaInfoCollection()
                {
                    new MetaInfo()
                    {
                        Label = "name",
                        Title = "name"
                    }
                }
            };
            PanoramioService.PoITypes.Add(mImagePoiStyle);
            AppState.DataServer.Services.Add(PanoramioService);
            Url = AppState.Config.Get("CustomPanoramio.Url", "");
            if (!String.IsNullOrEmpty(Url))
            {
                mWebClient = new WebClient();
                mWebClient.DownloadDataCompleted += MWebClient_DownloadDataCompleted;
                mWebClient.DownloadDataAsync(new Uri(Url));
                ReloadInMinutes =  AppState.Config.GetInt("CustomPanoramio.RefreshInMinutes", 15);
                if (ReloadInMinutes > 0)
                {
                    var ts = new TimeSpan(0, ReloadInMinutes, 0);
                    mTimer = new System.Timers.Timer(ts.Milliseconds);
                    mTimer.Elapsed += MTimer_Elapsed;
                    mTimer.AutoReset = false;
                }
            }


        }

      

        private void MTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            mWebClient.DownloadDataAsync(new Uri(Url));
        }

        private void MWebClient_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            if ((e.Cancelled) || (e.Error != null))
            {
                Thread.Sleep(5000);
                mWebClient.DownloadDataAsync(new Uri(Url));

            } else Caliburn.Micro.Execute.OnUIThread(() => ProcessGeoJsonFile(System.Text.Encoding.Default.GetString(e.Result)));
                
        }

        private void ProcessGeoJsonFile(string pJson)
        {
            try
            {
                string urlPrefix = AppState.Config.Get("CustomPanoramio.UrlPrefix", "");
                PanoramioService.PoIs.Clear();
                //var json = File.ReadAllText(@"C:\DEVELOPMENT\test.json");
                JObject root = JObject.Parse(pJson);
                foreach (var item in root["features"].Values<JObject>())
                {
                    var prop = item["properties"];
                    // prop["gpsTimeStr"]
                    var url = String.Format("{0}{1}", urlPrefix, prop["largeUrl"].ToString());
                    url = "http://mw2.google.com/mw-panoramio/photos/medium/2942843.jpg";
                    var p = new PoI
                    {
                        PoiTypeId = mImagePoiStyle.PoiId,
                        Service = PanoramioService,
                        PoiType = mImagePoiStyle,
                        /*Id = Guid.NewGuid(),*/
                        ContentId = Guid.NewGuid().ToString(),
                        
                        
                        Style = new PoIStyle { Icon = url },
                        Position = new Position(
                            Convert.ToDouble(item["geometry"]["coordinates"][0], CultureInfo.InvariantCulture),
                            Convert.ToDouble(item["geometry"]["coordinates"][1], CultureInfo.InvariantCulture)),
                    };

                    p.AllMedia.Add(
                        new Media()
                        {
                            PublicUrl = url,
                            LocalPath = url
                        });
                    PanoramioService.PoIs.Add(p);
                }

                PanoramioService.IsSubscribed = true;
                if (mTimer != null) mTimer.Start();
            }
            catch (Exception ex)
            {
                LogCs.LogException("CustomPanaramio failure", ex);

            }

            
        }



        public void Remove()
        {
            // AppState.ViewDef.MapControl.ExtentChanged -= MapControl_ExtentChanged;
            if (PanoramioService != null)
            {
                PanoramioService.Stop();
                AppState.DataServer.Services.Remove(PanoramioService);
                PanoramioService = null;
            }
            if (mWebClient != null)
            {
                mWebClient.DownloadDataCompleted -= MWebClient_DownloadDataCompleted;
                mWebClient = null;
                if (mTimer != null)
                {
                    mTimer.Stop();
                    mTimer = null;


                }
            }
            IsRunning = false;
        }

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                NotifyOfPropertyChange(() => Name);
            }
        }


        public string ImageUrl
        {
            get { return _imageUrl; }
            set
            {
                _imageUrl = value;
                NotifyOfPropertyChange(() => ImageUrl);
            }
        }


        public string Folder { get; set; }
    }
}
