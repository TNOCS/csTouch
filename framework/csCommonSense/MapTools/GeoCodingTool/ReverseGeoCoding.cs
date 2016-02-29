#region

using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using csShared.Utils;
using ESRI.ArcGIS.Client.Geometry;

#endregion

namespace csCommon.MapTools.GeoCodingTool
{
    public class ReverseGeoCoding
    {
        private static string baseUri = "http://maps.googleapis.com/maps/api/geocode/xml?latlng={0},{1}&sensor=false";

        //private bool _busy;
        private readonly WebClient wc = new WebClient();

        public TimeSpan MinInterval = new TimeSpan(0, 0, 0, 1);
        private DateTime lastRequest;

        public ReverseGeoCoding() {
            wc = new WebClient {Encoding = Encoding.UTF8};
            wc.DownloadStringCompleted += WcDownloadStringCompleted;
        }

        public event EventHandler<ReverseGeocodingCompletedEventArgs> Result;


        public bool RetrieveFormatedAddress(MapPoint pos, bool overule) {
            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    if (InternetConnection.IsConnected()) {
                        if (DateTime.Now < lastRequest.Add(MinInterval) && !overule) return;
                        if (wc.IsBusy && !overule) return;
                        if (wc.IsBusy) {
                            wc.CancelAsync();
                        }

                        var requestUri = string.Format(baseUri, pos.Y.ToString(CultureInfo.InvariantCulture),
                            pos.X.ToString(CultureInfo.InvariantCulture));

                        wc.DownloadStringAsync(new Uri(requestUri), pos);
                    }
                    else {
                        //AppStateSettings.Instance.FinishDownload(g);
                    }
                }
                catch (Exception e) {
                    Logger.Log("Geocoding", "Error retrieving geocoding data from google", e.Message, Logger.Level.Error);
                }
            });
            return true;
        }

        private void WcDownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e) {
            try {
                lastRequest = DateTime.Now;
                if (e.Error != null || e.Result == null) return;
                var pos = e.UserState as MapPoint;

                var xmlElm = XElement.Parse(e.Result);

                var status = (from elm in xmlElm.Descendants()
                    where elm.Name == "status"
                    select elm).FirstOrDefault();
                if (status.Value.ToLower() == "ok") {
                    var a = new Address(xmlElm.Descendants().ToList()[1]) {Position = pos};
                    var res = (from elm in xmlElm.Descendants()
                        where elm.Name == "formatted_address"
                        select elm).FirstOrDefault();
                    if (Result != null) {
                        Result(this, new ReverseGeocodingCompletedEventArgs(res.Value, e.Result, a));
                    }
                }
                else {
                    Console.WriteLine("No Address Found");
                }
            }
            catch (Exception er) {
                Logger.Log("Reverse Geocode", "Error parsing geocoding result", er.Message, Logger.Level.Error);
            }
        }
    }
}