using csShared;
using csShared.Utils;
using DataServer;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using System;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;

namespace csCommon.MapPlugins.Search
{
    public class GeoNamesSearch : BaseSearch
    {
        public override string Title
        {
            get { return "Geo"; }
        }

        public override bool IsOnline { get { return true; } }

        public static AppStateSettings AppState
        {
            get { return AppStateSettings.GetInstance(); }
        }

        public override void DoSearch(SearchPlugin plugin)
        {
            base.DoSearch(plugin);

            if (!string.IsNullOrEmpty(Key)) DoRequest();
        }

        public void DoRequest()
        {
            //var wm = new WebMercator();
            //var ext = (Envelope)wm.ToGeographic(AppState.ViewDef.MapControl.Extent);
            //var center = ext.GetCenter();
            //var radius = (int) (AppState.ViewDef.MapControl.Resolution*AppState.ViewDef.MapControl.ActualWidth);
            Plugin.IsLoading = true;
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    var wc = new WebClient();
                    string uri = "http://www.openstreetmap.org/geocoder/search_geonames?query=" + Key;
                    string res = wc.DownloadString(uri);

                    Application.Current.Dispatcher.Invoke(
                        delegate
                        {
                            lock (Plugin.ServiceLock)
                            {
                                ParseResult(res);
                            }
                            Plugin.IsLoading = false;
                        });
                }
                catch (Exception e)
                {
                    Logger.Log("Geocoding", "Error finding location", e.Message,
                        Logger.Level.Error,true);
                    Plugin.IsLoading = false;
                }
            });
        }

        public void ParseResult(string res)
        {
            if (res == string.Empty) return;
            var geoResult = Plugin.CreatePoiTypeResult("Geo", Colors.Orange);

            Plugin.SearchService.PoITypes.Add(geoResult);
            var count = 0;
            var doc = XDocument.Parse(res);
            foreach (var li in doc.Root.Elements())
            {
                count += 1;
                var a = li.Element("p").Element("a"); //.Attribute("data-lat").Value;
                var p = new PoI
                {
                    Name = a.Attribute("data-name").Value + a.Attribute("data-suffix").Value,
                    Service = Plugin.SearchService,
                    PoiTypeId = geoResult.ContentId,
                    PoiType = geoResult,
                    InnerText = count.ToString(CultureInfo.InvariantCulture),
                    Position = new Position(double.Parse(a.Attribute("data-lon").Value, CultureInfo.InvariantCulture),
                            double.Parse(a.Attribute("data-lat").Value, CultureInfo.InvariantCulture))
                };
                Plugin.SearchService.PoIs.Add(p);
            }
        }
    }
}