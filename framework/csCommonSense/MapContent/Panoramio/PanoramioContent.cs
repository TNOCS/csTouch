using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Media;
using Caliburn.Micro;
using csCommon.Types.DataServer.PoI;
using csShared;
using csShared.Geo;
using DataServer;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using IContent = csShared.Geo.Content.IContent;
using Media = DataServer.Media;

namespace csGeoLayers.Content.Panoramio
{
    public class PanoramioContent : PropertyChangedBase, IContent
    {
        private string _imageUrl;

        private Uri _location;
        private string _name;
        private bool isRunning;

        public bool IsOnline {
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

        #region IContent Members

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

        private SaveService service;
        private PoI image;

        public void Add()
        {
            //AppState.ViewDef.Layers.ChildLayers.Add(pl);
            IsRunning = true;

            service = new SaveService()
            {
                IsLocal = true,
                Name = "Panoramio",
                Id = Guid.NewGuid(),
                IsFileBased = false,
                StaticService = true,
                IsVisible = false,
                RelativeFolder = "Social Media"

            };

            service.Init(Mode.client, AppState.DataServer);
            // TODO Check met Arnoud of dit niet de folder moet zijn die in de configoffline staat?
            service.Folder = Directory.GetCurrentDirectory() + @"\PoiLayers\Social Media";
            service.InitPoiService();

            service.Settings.OpenTab = false;
            service.Settings.Icon = "brugwhite.png";
            service.AutoStart = true;

            image = new PoI
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
                }
            };

            image.AddMetaInfo("name", "name");
            //result.AddMetaInfo("height", "height", MetaTypes.number);
            //result.AddMetaInfo("width", "width", MetaTypes.number);
            //result.AddMetaInfo("description", "description");
            //result.AddMetaInfo("image", "image", MetaTypes.image);
            service.PoITypes.Add(image);

            AppState.DataServer.Services.Add(service);

            AppState.ViewDef.MapControl.ExtentChanged += MapControl_ExtentChanged;
            DownloadPhotos();
          
        }

        private DateTime _lastDownload;
        private WebMercator _mercator = new WebMercator();

        void MapControl_ExtentChanged(object sender, ESRI.ArcGIS.Client.ExtentEventArgs e)
        {
            DownloadPhotos();
        }

        private void DownloadPhotos()
        {
            
                _lastDownload = DateTime.Now;
                //Guid id = AppStateSettings.Instance.AddDownload("Download Panoramio","");
                var wc = new WebClient();
                var env = (Envelope)_mercator.ToGeographic(AppState.ViewDef.MapControl.Extent);
                wc.DownloadStringCompleted += WcDownloadStringCompleted;
                string url =
                    "http://www.panoramio.com/map/get_panoramas.php?order=popularity&set=public&from=0&to=40&minx=" +
                    Convert.ToString(env.XMin, CultureInfo.InvariantCulture) + "&miny=" +
                    Convert.ToString(env.YMin, CultureInfo.InvariantCulture) + "&maxx=" +
                    Convert.ToString(env.XMax, CultureInfo.InvariantCulture) + "&maxy=" +
                    Convert.ToString(env.YMax, CultureInfo.InvariantCulture) + "&callback=MyCallback";
                wc.DownloadStringAsync(new Uri(url));
            
            
        }

        private void WcDownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            // AppStateSettings.Instance.FinishDownload((Guid) e.UserState);
            if (e.Error != null) return;
            var n = new List<string>();

            string sReferences = e.Result;
            int iStart = sReferences.IndexOf("[{");
            int iLength = sReferences.IndexOf("}]") - iStart;
            if (iStart <= 1) return;
            string sPhotos = sReferences.Substring(iStart + 1, iLength - 1);
            ////string[] sPhotoArray = sPhotos.Split(new char[] { (char)10 });
            var sSeparator = new string[1];
            sSeparator[0] = "},";

            string[] sPhotoArray = sPhotos.Split(sSeparator, StringSplitOptions.RemoveEmptyEntries);

            //PrepLayer();

            var av = new List<string>();

            foreach (string sPhoto in sPhotoArray)
            {
                try
                {

                    var Id = GetValueFromTag(sPhoto, "photo_id");
                    var Name = GetValueFromTag(sPhoto, "photo_title");
                    var ImageUrl = GetValueFromTag(sPhoto, "photo_file_url");
                    var Height = Convert.ToInt16(GetValueFromTag(sPhoto, "height"));
                    var Width = Convert.ToInt16(GetValueFromTag(sPhoto, "width"));

                    if (service.PoIs.All(k => k.ContentId != Id))
                    {
                        var bc = new PoI
                        {
                            PoiTypeId = image.PoiId,
                            Service = service,
                            PoiType = image,
                            ContentId = Id,
                            Style = new PoIStyle {Icon = ImageUrl},
                            Position = new Position(Convert.ToDouble(GetValueFromTag(sPhoto, "longitude"),
                                CultureInfo.InvariantCulture),
                                Convert.ToDouble(GetValueFromTag(sPhoto, "latitude"),
                                    CultureInfo.InvariantCulture)),

                        };
                        var m = new Media();
                        m.PublicUrl = ImageUrl;
                        m.LocalPath = ImageUrl;
                        bc.AllMedia.Add(m);
                        service.PoIs.Add(bc);
                        av.Add(Id);
                    }

                    //CurrentFeatures.Add(pf);
                    //}
                    //else
                    //{
                    //    f.First().Attributes["visible"] = true;
                    //    //((PhotoFeature)f.First()).Enabled = true;
                    //}
                }
                catch
                {

                }

                //), "photo",
                //GetValueFromTag(sPhoto, "photo_title"), "", Double.Parse(GetValueFromTag(sPhoto, "latitude"), ciEnglish), Double.Parse(GetValueFromTag(sPhoto, "longitude"), ciEnglish),
                //16, Int32.Parse(GetValueFromTag(sPhoto, "owner_id")), GetValueFromTag(sPhoto, "owner_name"), DateTime.MinValue,
                //DateTime.MinValue, "http://www.panoramio.com");
            }

            var tbr = service.PoIs.Where(k => !av.Contains(k.ContentId)).ToList();
            foreach (var c in tbr) if (service.PoIs.Contains(c)) service.PoIs.Remove(c);

            //CleanLayer();

            //CurrentFeatures.RemoveAll(k => !((PhotoFeature) k).Enabled);
        }

        /// <summary>
        /// Obtiene el valor de una columna de la fila de resultados JSON
        /// </summary>
        /// <param name="input"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        private static string GetValueFromTag(string input, string tag)
        {
            try
            {
                int iEnd;
                int iStart = input.IndexOf(tag) + tag.Length;
                string s = input.Substring(iStart, 1);
                if (s == "\"") //texto
                {
                    iStart += 2;
                    iEnd = input.IndexOf("\"", iStart + 1);
                }
                else //num�rico
                {
                    iEnd = input.IndexOf(",", iStart + 1);
                }
                if (iEnd == -1) iEnd = input.Length;
                if (iEnd > iStart)
                {
                    string r = input.Substring(iStart, iEnd - iStart).Trim(',').Trim('\"');

                    return r;
                }
                return string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public void Remove()
        {
            AppState.ViewDef.MapControl.ExtentChanged -= MapControl_ExtentChanged;
            service.Stop();
            AppState.DataServer.Services.Remove(service);
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

        #endregion
    }
}