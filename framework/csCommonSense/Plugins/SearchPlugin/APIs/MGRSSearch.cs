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
    public class MGRSSearch : BaseSearch
    {

        public override string Title { get { return "MGRS"; } }

        public override bool IsOnline { get { return true; } }

        
        public AppStateSettings AppState { get { return AppStateSettings.GetInstance(); }}
        public override void DoSearch(SearchPlugin plugin)
        {
            base.DoSearch(plugin);
            
            if (!string.IsNullOrEmpty(Key)) DoRequest();
        }

        private void DoRequest()
        {
            Plugin.IsLoading = true;
            //var bagResult = Plugin.CreatePoiTypeResult("BAG", Colors.CornflowerBlue);

            var result = Plugin.CreatePoiTypeResult("MGRS", Colors.CornflowerBlue);
            result.Style.InnerTextColor = Colors.Black;
            Plugin.SearchService.PoITypes.Add(result);


            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    string mgrsInput = Key;
                    var pois = new ContentList();

                    double[] latLonResult = csCommon.Converters.MgrsConversion.convertMgrsToLatLon(mgrsInput);
                    double lat = latLonResult[0];  
                    double lon = latLonResult[1];   

                    var p = new PoI
                    {
                        Service = Plugin.SearchService,
                        InnerText = "1",
                        Name = mgrsInput,
                        Position = new Position(lon, lat),
                    };

                    p.UpdateEffectiveStyle();
                    pois.Add(p);

                    Application.Current.Dispatcher.Invoke(
                        delegate
                        {
                            lock (Plugin.ServiceLock)
                            {
                                foreach (var q in pois) {
                                    Plugin.SearchService.PoIs.Add(q);
                                }
                            }
                            Plugin.IsLoading = false;
                        });

                }
                catch (Exception e)
                {
                    Logger.Log("MGRS search", "Error finding MGRS location", e.Message, Logger.Level.Error, true);
                    Plugin.IsLoading = false;
                }
            });
        }
    }
}