using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Media;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using ESRI.ArcGIS.Client.Symbols;
using csShared;
using csShared.Geo;

namespace csGeoLayers.FlightTracker
{
    [Serializable]
    public class AirportLayer : SettingsGraphicsLayer
    {
        private readonly WebMercator _mercator = new WebMercator();
        //public event EventHandler Loaded;

        private string _url = "http://openflights.svn.sourceforge.net/viewvc/openflights/openflights/data/airports.dat";

        public override void Initialize()
        {
            ID = "Airports";
            base.Initialize();
            SpatialReference = new SpatialReference(4326);
            DownloadAirports();
            this.Clusterer = new FlareClusterer()
                                 {
                                     MaximumFlareCount = 5,
                                     FlareBackground = Brushes.Blue,
                                     FlareForeground = Brushes.White
                                 };

        }

        private Guid _id;
        

        private void DownloadAirports()
        {
            _id = AppStateSettings.Instance.AddDownload("Download Airports", "");
            AppStateSettings.Instance.MediaC.DownloadCompleted += MediaCDownloadCompleted;
            string s = AppStateSettings.Instance.MediaC.GetFile(_url, false);
            if (s != _url)
            {
                ParseFile(s);
            }
            else
            {
                    
            }

            
            var wc = new WebClient();
            var env = (Envelope) _mercator.ToGeographic(Map.Extent);
            //wc.DownloadStringCompleted += WcDownloadStringCompleted;
            string url = "http://openflights.svn.sourceforge.net/viewvc/openflights/openflights/data/airports.dat";
            wc.DownloadStringAsync(new Uri(url), _id);
        }

        void MediaCDownloadCompleted(string orig, string cached, string hashcode)
        {
            if (orig == _url)
            {
                AppStateSettings.Instance.MediaC.DownloadCompleted -= MediaCDownloadCompleted;                 
                ParseFile(cached);
            }
        }

        private void ParseFile(string cached)
        {
            string[] sReferences = File.ReadAllLines(cached);
            foreach (string ap in sReferences)
            {
                try
                {
                    string[] s = ap.Split(',');
                    var g = new Graphic();
                    var pd = new ResourceDictionary();
                    pd.Source = new Uri("csGeoLayers;component/csGeoLayers/FlightTracker/FTDictionary.xaml", UriKind.Relative);
                    g.Symbol = pd["MediumMarkerSymbol"] as SimpleMarkerSymbol;

                    var mp = new MapPoint((float) Convert.ToDouble(s[7], CultureInfo.InvariantCulture),
                                          (float) Convert.ToDouble(s[6], CultureInfo.InvariantCulture),
                                          new SpatialReference(4326));

                    g.Geometry = mp;
                    Graphics.Add(g);
                }
                catch (Exception)
                {
                }
            }
            AppStateSettings.Instance.FinishDownload(_id);
        }


       
        public override void StartSettings()
        {
            
        }
    }
}