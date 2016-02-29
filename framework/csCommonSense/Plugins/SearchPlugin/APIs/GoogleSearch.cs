using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using csShared.Utils;
using DataServer;

namespace csCommon.MapPlugins.Search
{
    public class GoogleSearch : BaseSearch
    {
        public override bool IsOnline{ get { return true; } }

        public override string Title { get { return "Locations"; } }

        public override void DoSearch(SearchPlugin plugin)
        {
            base.DoSearch(plugin);
            if (!string.IsNullOrEmpty(Key)) DoRequest();
        }

        public void DoRequest()
        {
            Plugin.IsLoading = true;
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    
                    var wc = new WebClient();
                    var uri = "https://maps.google.com/maps/api/geocode/xml?address=" + Key + "&sensor=false";
                    var res = wc.DownloadString(uri);
                    // "http://maps.google.com/maps/geo?output=xml&q=" + Keyword + "&key=ABQIAAAA61Zfghqchn1_2tR1VCcfbxRDK-cVyu9nXPtoS5FDSKel16cd2BS816uIQiq1y6oHZZ5qRowceF4eNA&oe=utf8&gl=nl"

                    Application.Current.Dispatcher.Invoke(
                        delegate
                        {
                            lock (Plugin.ServiceLock)
                            {
                                ParseResult(res);
                            }
                        });
                    Plugin.IsLoading = false;
                }
                catch (Exception e)
                {
                    Logger.Log("Geocoding", "Error finding location", e.Message, Logger.Level.Error,true);
                    Plugin.IsLoading = false;

                }
            });
        }

        public void ParseResult(string res) {
            if (string.IsNullOrEmpty(res)) return;
            try {
                var googleResult = Plugin.CreatePoiTypeResult("Google", Colors.Orange);
                googleResult.AddMetaInfo("Title", "title");
                

                Plugin.SearchService.PoITypes.Add(googleResult);
                var doc = XDocument.Parse(res);
                var kml = doc.Elements().First();
                var status = kml.Element("status").Value;
                if (status != "OK") return;
                var count = 0;
                foreach (var xe in kml.Elements("result")) {
                    count ++;
                    var ed = xe.GetFirstElement("geometry");
                    if (ed == null) continue;
                    var loc = ed.GetFirstElement("location");
                    var lat = double.Parse(loc.GetFirstElement("lat").Value,
                        CultureInfo.InvariantCulture);

                    var lng = double.Parse(loc.GetFirstElement("lng").Value,
                        CultureInfo.InvariantCulture);

                    var p = new PoI {
                        Service = Plugin.SearchService,
                        InnerText = count.ToString(),
                        PoiTypeId = googleResult.ContentId,
                        PoiType = googleResult,
                        Position = new Position(lng, lat)
                    };

                    var bytes = Encoding.Default.GetBytes(xe.GetFirstElement("formatted_address").Value);
                    
                    p.Name = Encoding.UTF8.GetString(bytes);
                    p.Labels["title"] = p.Name;
                    p.UpdateEffectiveStyle();
                    Plugin.SearchService.PoIs.Add(p);

                    //var llb = ed.GetFirstElement("viewport");
                    //var sw = llb.Element("southwest");
                    //var ne = llb.Element("northeast");
                    //double north = Convert.ToDouble(ne.Element("lat").Value,
                    //    CultureInfo.InvariantCulture);
                    //double south = Convert.ToDouble(sw.Element("lat").Value,
                    //    CultureInfo.InvariantCulture);
                    //double west = Convert.ToDouble(sw.Element("lng").Value,
                    //    CultureInfo.InvariantCulture);
                    //double east = Convert.ToDouble(ne.Element("lng").Value,
                    //    CultureInfo.InvariantCulture);
                    //string [] locs = xe.GetFirstElement("Image").GetFirstElement("coordinates").Value.Split(',');
                    //if (locs.Count() ==3)
                    //{
                    //    gcsr.Location = new KmlPoint { Latitude=(float)Convert.ToDouble(locs[1],CultureInfo.InvariantCulture),
                    //                  Longitude=(float)Convert.ToDouble(locs[0],CultureInfo.InvariantCulture),};
                    //    Result.Add(gcsr);
                    //}
                }
            }
            catch (Exception e) {
                Logger.Log("Geocoding", "Error doing geocoding request : " + e.Message, "", Logger.Level.Error);
            }
        }
    }
}