using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Timers;
using System.Windows;
using System.Xml.Linq;
using Caliburn.Micro;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using ESRI.ArcGIS.Client.Symbols;
using csShared;
using csShared.Geo;

namespace csGeoLayers.FlightTracker
{
    [Serializable]
    public class FlightTrackerLayer : SettingsGraphicsLayer
    {
        private readonly WebMercator _mercator = new WebMercator();
        //public event EventHandler Loaded;

        Timer _dt = new Timer();

        Dictionary<string,Plane> _planes = new Dictionary<string, Plane>();

        public override void Initialize()
        {
            ID = "Netherlands Flight Tracker";
            base.Initialize();
            SpatialReference = new SpatialReference(4326);
            _dt.Interval = 2000;
            _dt.Elapsed += (e, s) => DownloadData();
            _dt.Start();

        }

        
        private void DownloadData()
        {
            //if (Visible)
            {
                Guid id = AppStateSettings.Instance.AddDownload("Download Flight Tracker", "");
                var wc = new GZipWebClient();
                //var env = (Envelope) _mercator.ToGeographic(Map.Extent);
                wc.DownloadStringCompleted += WcDownloadStringCompleted;
                wc.Headers.Add("Referer", "http://www.geluidsnet.nl/includes/flash/SensorNet-014.swf?Server=wms.sensornw.net/&WMS=SenSorNetNL&GML=http://xml.sensornw.net/geluidsnet&MapIndex=LR&BBox=3.2,50.5,6.2,53.5&WMSBBox=2.7,49,6.7,54.7&MapIndexBBox=2.7,49.6,6.7,54.7&MapIndexHeight=100&MapIndexWidth=100&MapIndexWMS=SensorNet&MaxZoomIn=10000&CustomLegend=/includes/flash/legenda.png&Layers=Geluidsnet,lage_vliegtuigen,hoge_vliegtuigenSymbols,Plaatsnamen,Straatnamen,Paden,Wegen,Hoofdwegen,Snelwegen,Spoorwegen,Vliegvelden,Water,Groen,Industrie,Stedelijk,Borders,NL,Roads,Railroads,Waterbodies,Urban,Countries");
                wc.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
                
                
                string url = "http://xml.sensornw.net/geluidsnet?refresh=" + Convert.ToInt64(
                    (DateTime.Now.AddHours(-2) - new DateTime(1970, 1, 1)).TotalMilliseconds);
                 
                wc.DownloadStringAsync(new Uri(url), id);
            }
        }

        private void PrepLayer()
        {
            foreach (Graphic g in Graphics)
            {
                g.Attributes["Visible"] = false;
            }
        }

        private void CleanLayer()
        {
           
                var tbd = Graphics.Where(k => (bool)k.Attributes["Visible"] == false).ToList();
                foreach (var v in tbd)
                    Graphics.Remove(v);
            
        }



        private void WcDownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            AppStateSettings.Instance.FinishDownload((Guid) e.UserState);
            if (e.Error == null)
            {
                var n = new List<string>();
                string s = e.Result.Replace("</lage_vliegtuigen>", "</lage_vliegtuigen></result>").Replace(
                    "<Geluidsnet", "<result><Geluidsnet");

                XDocument xdoc = XDocument.Parse(s);
                Dispatcher.BeginInvoke(new System.Action(delegate
                            {
                                PrepLayer();
                                foreach (XElement p in xdoc.Element("result").Element("hoge_vliegtuigen").Elements("Plane"))
                                {
                                    UpdatePlane(p);

                                }

                                foreach (XElement p in xdoc.Element("result").Element("lage_vliegtuigen").Elements("Plane"))
                                {
                                    UpdatePlane(p);
                                }
                                CleanLayer();
                            }));


            }
        }

        private void UpdatePlane(XElement p)
        {
            try
            {
                string cs = p.Element("Callsign").Value;
                Plane plane = new Plane() {CallSign = cs};
                if (!_planes.ContainsKey(cs))
                {
                    plane.ParseElement(p);
                    plane.Graphic = new Graphic();
                    plane.Graphic.Attributes["id"] = plane.CallSign;
                    plane.Graphic.Attributes["plane"] = plane;
                    plane.Graphic.Attributes["Angle"] = plane.Angle;                    
                    plane.Graphic.Geometry = plane.Position;
                    var pd = new ResourceDictionary();
                    pd.Source = new Uri("csGeoLayers;component/csGeoLayers/Content/FlightTracker/FTDictionary.xaml", UriKind.Relative);
                    plane.Graphic.Symbol = pd["PlaneSymbol"] as MarkerSymbol;
                    Graphics.Add(plane.Graphic);                    
                    _planes.Add(plane.CallSign, plane);
                }
                else
                {
                    plane = _planes[cs];
                    plane.ParseElement(p);                    
                    plane.Graphic.Attributes["plane"] = plane;
                   
                    
                    plane.Graphic.Attributes["Angle"] = plane.Angle;
                    plane.Graphic.Geometry = plane.Position;
                }
                plane.Graphic.Attributes["Visible"] = true;
                plane.Enabled = true;
            }
            catch (Exception)
            {
                //throw;
            }
        }

        /// <summary>
        /// Obtiene el valor de una columna de la fila de resultados JSON
        /// </summary>
        /// <param name="input"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        private string GetValueFromTag(string input, string tag)
        {
            try
            {
                int iEnd;
                int iStart = input.IndexOf(tag) + tag.Length;
                string s = input.Substring(iStart, 1);
                if (s == "\"") //texto
                {
                    iStart += 2;
                    iEnd = input.IndexOf("\"", iStart+1);
                }
                else //num�rico
                {
                    iEnd = input.IndexOf(",", iStart+1);
                }
                string r = input.Substring(iStart, iEnd - iStart).Trim(',').Trim('\"');                
                return r;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public override void StartSettings()
        {
            
        }

        internal void Stop()
        {
            _dt.Stop();
            CleanLayer();
        }
    }

    public class Plane : PropertyChangedBase
    {

        private bool _enabled;

        public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }
        

        private int _altitude;

        public int Altitude
        {
            get { return _altitude; }
            set { _altitude = value; NotifyOfPropertyChange(()=>Altitude); }
        }

        private int _speed;

        public int Speed
        {
            get { return _speed; }
            set { _speed = value; NotifyOfPropertyChange(()=>Speed); }
        }

        private MapPoint _position;

        public MapPoint Position
        {
            get { return _position; }
            set { _position = value; NotifyOfPropertyChange(()=>Position); }
        }

        private string _callSign;

        public string CallSign
        {
            get { return _callSign; }
            set { _callSign = value; NotifyOfPropertyChange(()=>CallSign); }
        }

        private Graphic _graphic;

        public Graphic Graphic
        {
            get { return _graphic; }
            set { _graphic = value; }
        }

        private int _angle;

        public int Angle
        {
            get { return _angle; }
            set { _angle = value; NotifyOfPropertyChange(()=>Angle); }
        }

        private string _operator;

        public string Operator
        {
            get { return _operator; }
            set { _operator = value; NotifyOfPropertyChange(()=>Operator); }
        }

        private string _type;

        public string Type
        {
            get { return _type; }
            set { _type = value; NotifyOfPropertyChange(()=>Type); }
        }

        private string _registration;

        public string Registration
        {
            get { return _registration; }
            set { _registration = value; }
        }
        
        
        
        

        public void ParseElement(XElement p)
        {
            Altitude = int.Parse(p.Element("Altitude").Value.Replace(" m", ""));
            Speed = int.Parse(p.Element("Speed").Value.Replace(" km/h", ""));
            Angle = int.Parse(p.Element("_Angle").Value);
            Operator = p.Element("Operator").Value;
            Type = p.Element("Type").Value;
            Registration = p.Element("Registration").Value;


            string[] pos = p.Element("Point").Element("Coordinates").Value.Split(',');
            Position = new MapPoint(double.Parse(pos[0], CultureInfo.InvariantCulture), double.Parse(pos[1], CultureInfo.InvariantCulture), new SpatialReference(4326));


           
        }
        
        
        
    }

    public class GZipWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            return request;
        }
    }

    
}