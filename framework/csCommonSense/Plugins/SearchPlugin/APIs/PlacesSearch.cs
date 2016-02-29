using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using csShared;
using csShared.Utils;
using DataServer;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;

namespace csCommon.MapPlugins.Search
{
    public class PlacesSearch : BaseSearch
    {

        public override string Title { get { return "Places"; } }

        public override bool IsOnline { get { return true; } }

        
        public AppStateSettings AppState { get { return AppStateSettings.GetInstance(); }}
        public override void DoSearch(SearchPlugin plugin)
        {
            base.DoSearch(plugin);
            
            if (!string.IsNullOrEmpty(Key)) DoRequest();
        }

        public void DoRequest()
        {
            Plugin.IsLoading = true;
            var wm = new WebMercator();
           var ext = (Envelope)wm.ToGeographic(AppState.ViewDef.MapControl.Extent);
           var center = ext.GetCenter();
           var radius = (int)(AppState.ViewDef.MapControl.Resolution * AppState.ViewDef.MapControl.ActualWidth);

           ThreadPool.QueueUserWorkItem(delegate
           {
               try
               {
                   var wc = new WebClient();


                   //string uri = "https://maps.googleapis.com/maps/api/place/textsearch/xml?query=" + SearchKey + "&sensor=true&key=AIzaSyB1qBfe0nLX_46z0K7G5LS4DLp-0GxazVM";
                   string uri =
                       "https://maps.googleapis.com/maps/api/place/nearbysearch/xml?location=" + center.Y.ToString(CultureInfo.InvariantCulture) + "," + center.X.ToString(CultureInfo.InvariantCulture) + "&radius=" + radius + "&sensor=false&keyword=" + Key + "&key=AIzaSyB1qBfe0nLX_46z0K7G5LS4DLp-0GxazVM";
                   string res = wc.DownloadString(uri);

                   Application.Current.Dispatcher.Invoke(
                       delegate {
                           {
                               lock (Plugin.ServiceLock)
                               {
                                   ParseResult(res);
                               }
                               Plugin.IsLoading = false;
                           } });
               }
               catch (Exception e)
               {
                   Plugin.IsLoading = false;
                   Logger.Log("Geocoding", "Error finding location", e.Message,
                       Logger.Level.Error,true);
               }
           });
        }

        public void ParseResult(string res)
        {
            if (res != string.Empty)
            {
                try
                {
                    var placeResult = Plugin.CreatePoiTypeResult("Place",Colors.Orange);
                    placeResult.DrawingMode = DrawingModes.Image;
                    placeResult.AddMetaInfo("vicinity", "vicinity");

                    Plugin.SearchService.PoITypes.Add(placeResult);
                    XDocument doc = XDocument.Parse(res);
                    XElement kml = doc.Elements().First();
                    string status = kml.Element("status").Value;
                    if (status == "OK")
                    {
                        int count = 0;
                        foreach (XElement xe in kml.Elements("result"))
                        {
                            count += 1;
                            XElement ed = xe.GetFirstElement("geometry");
                            XElement loc = ed.GetFirstElement("location");

                            var p = new PoI()
                            {
                                Name = xe.GetFirstElement("name").Value,
                                Service = Plugin.SearchService,
                                InnerText = count.ToString(),
                                Style = new PoIStyle() { Icon = xe.GetFirstElement("icon").Value},
                                PoiTypeId = placeResult.ContentId,
                                PoiType = placeResult,
                                Position =
                                    new Position(double.Parse(loc.GetFirstElement("lng").Value, CultureInfo.InvariantCulture), double.Parse(loc.GetFirstElement("lat").Value, CultureInfo.InvariantCulture))
                            };
                            p.Labels.Add("vicinity", xe.GetFirstElement("vicinity").Value);
                            Plugin.SearchService.PoIs.Add(p);


                            
                            //string [] locs = xe.GetFirstElement("Image").GetFirstElement("coordinates").Value.Split(',');
                            //if (locs.Count() ==3)
                            //{
                            //    gcsr.Location = new KmlPoint { Latitude=(float)Convert.ToDouble(locs[1],CultureInfo.InvariantCulture),
                            //                  Longitude=(float)Convert.ToDouble(locs[0],CultureInfo.InvariantCulture),};
                            //    Result.Add(gcsr);
                            //}
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Log("Geocoding", "Error doing geocoding request : " + e.Message, "", Logger.Level.Error);
                }
                
            }

           
        }


       
    }
}