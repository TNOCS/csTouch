using System;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using csShared.Utils;
using DataServer;

namespace csCommon.MapPlugins.Search
{
    public class WikiSearch : BaseSearch
    {
        public override string Title
        {
            get { return "Wiki"; }
        }

        public override bool IsOnline { get { return true; } }

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
                    string uri =
                        "http://api.geonames.org/wikipediaSearch?q=" + Key + "&maxRows=25&username=damylen";
                    string res =
                        wc.DownloadString(uri);

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
            if (res != string.Empty)
            {
                try
                {

                   

                    var wikiResult = Plugin.CreatePoiTypeResult("Wiki", Colors.Aqua);
                    wikiResult.Style.InnerTextColor = Colors.Black;
                    wikiResult.AddMetaInfo("Summary", "summary");
                    wikiResult.AddMetaInfo("Feature", "feature");

                    Plugin.SearchService.PoITypes.Add(wikiResult);


                    XDocument doc = XDocument.Parse(res);
                    XElement kml = doc.Root;
                    XElement status = kml.Element("status");
                    if (status == null)
                    {
                        int count = 0;
                        foreach (XElement xe in kml.Elements("entry"))
                        {
                            try
                            {
                                count += 1;
                                var p = new PoI
                                {
                                    Name = xe.GetFirstElement("title").Value,
                                    Service = Plugin.SearchService,
                                    PoiTypeId = wikiResult.ContentId,
                                    InnerText = count.ToString(),
                                    PoiType = wikiResult,
                                    Position =
                                        new Position(
                                            double.Parse(xe.GetFirstElement("lng").Value, CultureInfo.InvariantCulture),
                                            double.Parse(xe.GetFirstElement("lat").Value, CultureInfo.InvariantCulture))
                                };
                                if (xe.GetFirstElement("summary") != null)
                                    p.Labels.Add("summary", xe.GetFirstElement("summary").Value);
                                if (xe.GetFirstElement("feature") != null)
                                    p.Labels.Add("feature", xe.GetFirstElement("feature").Value);

                                Plugin.SearchService.PoIs.Add(p);
                            }
                            catch (Exception e)
                            {
                                Logger.Log("Geocoding", "Error doing wikipedia search request : " + e.Message, "",
                                    Logger.Level.Error);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Log("Geocoding", "Error doing wikipedia search request : " + e.Message, "",
                        Logger.Level.Error);
                }
            }
        }
    }
}