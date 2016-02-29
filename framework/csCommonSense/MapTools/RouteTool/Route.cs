using Caliburn.Micro;
using csShared;
using csShared.Geo;
using csShared.Utils;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using ESRI.ArcGIS.Client.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Timers;
using System.Web;
using System.Windows;
using System.Windows.Media;
using PointCollection = ESRI.ArcGIS.Client.Geometry.PointCollection;

namespace csCommon.MapPlugins.MapTools.RouteTool
{
    public class Route : PropertyChangedBase
    {
        public GroupLayer Layer;
        public Graphic _start;
        public Graphic _finish;

        public Graphic _line;

        public csPoint Start;
        public csPoint Finish;

        public GraphicsLayer MLayer;

        private bool _firstMovement = true;

        private string mode = "Driving";

        public string Mode
        {
            get { return mode; }
            set { mode = value; }
        }


        private double _distance;

        public double Distance
        {
            get { return _distance; }
            set { _distance = value; NotifyOfPropertyChange(() => Distance); }
        }

        private Timer GeoTimer;

        private bool Moved = false;

        private GoogleDirections lastDirection = null;
        private DateTime lastUpdated;

        private GoogleDirections _directions;

        public GoogleDirections Directions { get { return _directions; } set { _directions = value; NotifyOfPropertyChange(() => Directions); } }

        private DateTime startTime = DateTime.Now;

        public double GetDistance()
        {
            var w = new WebMercator();
            var p1 = w.ToGeographic(Start.Mp) as MapPoint;
            var p2 = w.ToGeographic(Finish.Mp) as MapPoint;
            var pLon1 = p1.X;
            var pLat1 = p1.Y;
            var pLon2 = p2.X;
            var pLat2 = p2.Y;

            var dist = CoordinateUtils.Distance(pLat1, pLon1, pLat2, pLon2, 'K');
            return dist;//Math.Sqrt((deltaX*deltaX) + (deltaY*deltaY));
        }

        public void Init(GroupLayer gl, MapPoint start, MapPoint finish, ResourceDictionary rd)
        {
            Start = new csPoint() { Mp = start };
            Finish = new csPoint() { Mp = finish };
            MLayer = new GraphicsLayer() { ID = Guid.NewGuid().ToString() };
            _start = new Graphic();
            _finish = new Graphic();
            _line = new Graphic();

            LineSymbol ls = new LineSymbol() { Color = Brushes.Black, Width = 4 };
            _line.Symbol = ls;
            //UpdateLine();

            MLayer.Graphics.Add(_line);

            _start.Geometry = start;
            _start.Attributes["position"] = start;

            _start.Symbol = rd["Start"] as Symbol;
            _start.Attributes["finish"] = _finish;
            _start.Attributes["start"] = _start;
            _start.Attributes["line"] = _line;
            _start.Attributes["state"] = "start";
            _start.Attributes["measure"] = this;
            _start.Attributes["menuenabled"] = true;
            MLayer.Graphics.Add(_start);

            _finish.Geometry = finish;
            _finish.Attributes["position"] = finish;
            _finish.Symbol = rd["Finish"] as Symbol;
            _finish.Attributes["finish"] = _finish;
            _finish.Attributes["menuenabled"] = true;
            _finish.Attributes["start"] = _start;
            _finish.Attributes["line"] = _line;
            _finish.Attributes["measure"] = this;
            _finish.Attributes["state"] = "finish";
            MLayer.Graphics.Add(_finish);



            Layer.ChildLayers.Add(MLayer);
            MLayer.Initialize();

            //AppStateSettings.Instance.ViewDef.MapManipulationDelta += ViewDef_MapManipulationDelta;

            GeoTimer = new Timer();

            GeoTimer.Elapsed += GeoTimer_Elapsed;
            GeoTimer.Interval = 100;

            GeoTimer.Start();
        }


        private MapPoint start;
        private MapPoint end;

        void GeoTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var web = new WebMercator();
            var mps = web.ToGeographic(_start.Geometry) as MapPoint;
            var mpf = web.ToGeographic(_finish.Geometry) as MapPoint;
            if (
                (Moved &&
                lastUpdated.AddSeconds(1) < DateTime.Now) &&
                (startTime.AddSeconds(3) < DateTime.Now) &&

                (start == null || end == null || start.X != mps.X || start.Y != mps.Y || end.X != mpf.X || end.Y != mpf.Y))
            {
                start = mps;
                end = mpf;
                lastUpdated = DateTime.Now;

                var gd = DirectionsUtils.GetDirections(mps.Y.ToString(CultureInfo.InvariantCulture) + "," + mps.X.ToString(CultureInfo.InvariantCulture),
                    mpf.Y.ToString(CultureInfo.InvariantCulture) + "," + mpf.X.ToString(CultureInfo.InvariantCulture), mode = Mode);
                if (gd != null)
                {
                    if (gd.Directions != null)
                    {
                        UpdateLine(gd);
                    }

                    Directions = gd;
                }
                Moved = false;


            }

            if (Moved && lastDirection != null)
            {
                //UpdateLine(lastDirection);

            }
        }


        static void ViewDef_MapManipulationDelta(object sender, EventArgs e)
        {
            //UpdateLine(gd);
        }

        private const int BinaryChunkSize = 5;
        private const int MinAscii = 63;

        /// <summary>
        /// decodes the cuurent chunk into a single integer value
        /// </summary>
        /// <param name="encoded">the complete encodered string</param>
        /// <param name="startindex">the current position in that string</param>
        /// <param name="finishindex">output - the position we end up in that string</param>
        /// <returns>the decoded integer</returns>
        private static int DecodePoint(string encoded, int startindex, out int finishindex)
        {
            int b;
            var shift = 0;
            var result = 0;
            do
            {
                //get binary encoding
                b = Convert.ToInt32(encoded[startindex++]) - MinAscii;
                //binary shift
                result |= (b & 0x1f) << shift;
                //move to next chunk
                shift += BinaryChunkSize;
            } while (b >= 0x20); //see if another binary value
            //if negivite flip
            var dlat = (((result & 1) > 0) ? ~(result >> 1) : (result >> 1));
            //set output index
            finishindex = startindex;
            return dlat;
        }

        public static List<KmlPoint> DecodeLatLong(string encoded)
        {
            List<KmlPoint> locs = new List<KmlPoint>();

            int index = 0;
            int lat = 0;
            int lng = 0;

            int len = encoded.Length;
            while (index < len)
            {
                lat += DecodePoint(encoded, index, out index);
                lng += DecodePoint(encoded, index, out index);

                locs.Add(new KmlPoint((lng * 1e-5), (lat * 1e-5)));
            }

            return locs;
        }

        internal void UpdateLine(GoogleDirections gd)
        {
            Execute.OnUIThread(() =>
            {
                var m = new WebMercator();

                var ps = DecodeLatLong(gd.Directions.Polyline.points);

                var pl = new ESRI.ArcGIS.Client.Geometry.Polyline {Paths = new ObservableCollection<PointCollection>()};
                var pc = new PointCollection();
                foreach (var p in ps)
                {
                    pc.Add((MapPoint) m.FromGeographic(new MapPoint(p.Longitude, p.Latitude)));
                }

                pl.Paths.Add(pc);
                _line.Geometry = pl;
                _line.SetZIndex(0);
            });
        }

        internal void Remove()
        {
            Layer.ChildLayers.Remove(MLayer);
        }

        internal void UpdatePoint(string state, MapPoint geometry)
        {
            Moved = true;

            switch (state)
            {
                case "start":

                    if (_firstMovement)
                    {
                        Finish.Mp.X += geometry.X - Start.Mp.X;
                        Finish.Mp.Y += geometry.Y - Start.Mp.Y;
                    }
                    Start.Mp = geometry;
                    _start.Geometry = geometry;
                    break;
                case "finish":
                    _firstMovement = false;
                    Finish.Mp = geometry;
                    _finish.Geometry = geometry;
                    break;
            }

            //UpdateLine();
            //Distance = GetDistance();
        }

        internal void StartPlay()
        {

        }
    }

    public class GeoCoordinate
    {
        public GeoCoordinate(double lat, double lon, int lev)
        {
            this.lat = lat;
            this.lon = lon;
            this.lev = lev;
        }
        public double lat;
        public double lon;
        public int lev;
    }
    public class Status
    {
        public Int64 code;
        public string request;
    }
    public class Thoroughfare
    {
        public string ThoroughfareName;
    }
    public class PostalCode
    {
        public string PostalCodeNumber;
    }
    public class DependentLocality
    {
        public string DependentLocalityName;
        public Thoroughfare Thoroughfare;
        public PostalCode PostalCode;
    }
    public class Locality
    {
        public string LocalityName;
        public Thoroughfare Thoroughfare;
        public PostalCode PostalCode;
        public DependentLocality DependentLocality;
    }

    public class SubAdministrativeArea
    {
        public string SubAdministrativeAreaName;
        public Locality Locality;
    }

    public class AdministrativeArea
    {
        public string AdministrativeAreaName;
        public SubAdministrativeArea SubAdministrativeArea;
        public Locality Locality;
    }

    public class Country
    {
        public string CountryNameCode;
        public string CountryName;
        public AdministrativeArea AdministrativeArea;
    }

    public class Point
    {
        public double[] coordinates;
    }
    
    public class AddressDetails
    {
        public Country Country;
        public Int64 Accuracy;
    }
    
    public class Placemark
    {
        public string id;
        public string address;
        public AddressDetails AddressDetails;
        public Point Point;
    }
    
    public class Distance
    {
        public string meters;
        public string html;
    }

    public class Duration
    {
        public string seconds;
        public string html;
    }

    public class Directions
    {
        public string copyrightsHtml;
        public string SummaryHtml;
        public Distance Distance;
        public Duration Duration;
        public GRoute[] Routes;
        public Polyline Polyline;
    }

    public class Step
    {
        public string descriptionHtml;
        public Distance Distance;
        public Duration Duration;
        public Point Point;
        public Int64 polylineIndex;
    }

    public class Polyline
    {
        public string id;
        public string points;
        public string levels;
        public Int64 numLevels;
        public Int64 zoomFactor;
    }

    public class GRoute
    {
        public Distance Distance;
        public Duration Duration;
        public string summaryHtml;
        public Step[] Steps;
        public Point End;
        public Int64 polylineEndIndex;
    }

    public class GoogleDirections
    {
        public string name;
        public string Status;
        public Placemark[] Placemark;
        public Directions Directions;
    }

    public class GoogleGeocodes
    {
        public string name;
        public Status Status;
        public Placemark[] Placemark;
    }

    public static class DirectionsUtils
    {
        public static GoogleDirections GetDirections(string addressA, string addressB, string mode, string wayPoints = "")
        {
            var sbUrl = new StringBuilder();
            //sbUrl.Append("http://maps.google.com/maps/nav?output=js&oe=utf8&q=");
            sbUrl.Append("http://maps.googleapis.com/maps/api/directions/json?");
            sbUrl.AppendFormat("origin={0}", HttpUtility.UrlEncode(addressA));
            sbUrl.AppendFormat("&destination={0}", HttpUtility.UrlEncode(addressB));
            if (!string.IsNullOrEmpty(wayPoints))
                sbUrl.AppendFormat("&waypoints={0}", HttpUtility.UrlEncode(wayPoints));
            sbUrl.Append("&sensor=false&mode=" + mode);
            var googleUrl = new Uri(sbUrl.ToString());
            var g = AppStateSettings.Instance.AddDownload(googleUrl.ToString(), "routing");

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(googleUrl);
                var response = (HttpWebResponse)request.GetResponse();
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var gResponse = reader.ReadToEnd();
                    dynamic directions = Newtonsoft.Json.JsonConvert.DeserializeObject(gResponse);
                    var route = directions.routes[0];
                    string points = route.overview_polyline.points;

                    var totMeters = 0;
                    var totSeconds = 0;
                    foreach (var leg in route.legs) {
                        int result;
                        string d = leg.distance.value;
                        if (int.TryParse(d,  NumberStyles.Number, CultureInfo.InvariantCulture, out result)) totMeters  += result;
                        string s = leg.duration.value;
                        if (int.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out result)) totSeconds += result;
                    }
                    var gd = new GoogleDirections {
                        Directions = new Directions {
                            Distance = new Distance {
                                //html = leg.distance.text,
                                meters = totMeters.ToString(CultureInfo.InvariantCulture)
                            },
                            Duration = new Duration {
                                //html = leg.duration.text,
                                seconds = totSeconds.ToString(CultureInfo.InvariantCulture)
                            }, 
                            SummaryHtml = route.summary.Value,
                            Polyline = new Polyline {points = points}
                        }
                    };
                    return gd;
                }
            }
            catch (Exception e)
            {
                Logger.Log("Routing Tool", "Error downloading routing info", e.Message + ": " + googleUrl, Logger.Level.Error);
            }
            finally
            {
                AppStateSettings.Instance.FinishDownload(g);
            }


            return null;
        }

        public static IEnumerable<KmlPoint> DecodeLatLong(string encoded)
        {
            var locs = new List<KmlPoint>();

            var index = 0;
            var lat = 0;
            var lng = 0;

            var len = encoded.Length;
            while (index < len)
            {
                lat += DecodePoint(encoded, index, out index);
                lng += DecodePoint(encoded, index, out index);

                locs.Add(new KmlPoint((lng * 1e-5), (lat * 1e-5)));
            }

            return locs;
        }

        private const int BinaryChunkSize = 5;
        private const int MinAscii = 63;

        /// <summary>
        /// decodes the cuurent chunk into a single integer value
        /// </summary>
        /// <param name="encoded">the complete encodered string</param>
        /// <param name="startindex">the current position in that string</param>
        /// <param name="finishindex">output - the position we end up in that string</param>
        /// <returns>the decoded integer</returns>
        public static int DecodePoint(string encoded, int startindex, out int finishindex)
        {
            int b;
            var shift = 0;
            var result = 0;
            do
            {
                //get binary encoding
                b = Convert.ToInt32(encoded[startindex++]) - MinAscii;
                //binary shift
                result |= (b & 0x1f) << shift;
                //move to next chunk
                shift += BinaryChunkSize;
            } while (b >= 0x20); //see if another binary value
            //if negivite flip
            var dlat = (((result & 1) > 0) ? ~(result >> 1) : (result >> 1));
            //set output index
            finishindex = startindex;
            return dlat;
        }

    }

}