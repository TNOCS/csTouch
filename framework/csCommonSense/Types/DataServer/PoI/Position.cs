using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Caliburn.Micro;
using csCommon.Types.DataServer.Interfaces;
using DocumentFormat.OpenXml.Drawing;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using Gavaghan.Geodesy;
using Newtonsoft.Json.Linq;
using ProtoBuf;

namespace DataServer
{
    public interface ITrackLog : IConvertibleCsv
    {
        DateTime Date { get; set; }
    }

    public class TrackLog : ITrackLog
    {
        public string Log { get; set; }
        public string Parameter { get; set; }
        public DateTime Date { get; set; }


        public string ToCsv(char separator = ',')
        {
            return "#" + Log + separator + Parameter + separator + Date.Ticks;;
        }

        public IConvertibleCsv FromCsv(string ln, char separator = ',', bool newObject = true)
        {
            TrackLog ret = (newObject) ? new TrackLog() : this;

            string[] l = ln.TrimStart('#').Split(separator);
            ret.Log = l[0];
            ret.Parameter = l[1];
            ret.Date = new DateTime(long.Parse(l[2]));

            return ret;
        }
    }

    [DebuggerDisplay(
        "Position (lat, long, alt, speed, course, elevation) = ({Latitude}, {Longitude}, {Altitude}, {Speed}, {Course}, {Elevation})"
        )]
    [ProtoContract]
    public class Position : PropertyChangedBase, IXmlSerializable, ITrackLog, IConvertibleGeoJson
    {
        public const string LAT_LABEL = "lat";
        public const string LONG_LABEL = "lon";
        public const string ALT_LABEL = "alt";

        private double accuracy;

        private double altitude;
        private double course;
        private double elevation;
        private double latitude;
        private double longitude;
        private double speed;

        public Position()
        {
        }

        public Position(double longitude, double latitude)
        {
            var prevNotifying = IsNotifying;
            IsNotifying = false;
            Latitude = latitude;
            Longitude = longitude;
            IsNotifying = prevNotifying;
        }

        public Position(double longitude, double latitude, double altitude)
            : this(longitude, latitude)
        {
            var prevNotifying = IsNotifying;
            IsNotifying = false;
            Altitude = altitude;
            IsNotifying = prevNotifying;
        }

        public Position(double longitude, double latitude, double altitude, double speed, double course)
            : this(longitude, latitude, altitude)
        {
            var prevNotifying = IsNotifying;
            IsNotifying = false;
            Speed = speed;
            Course = course;
            IsNotifying = prevNotifying;
        }

        public Position(string csvString, char separator = ',')
        {
            var prevNotifying = IsNotifying;
            IsNotifying = false;
            FromCsv(csvString);
            IsNotifying = prevNotifying;
        }

        /// <summary>
        ///     Gets or sets the latitude in degrees.
        /// </summary>
        /// <value>
        ///     The latitude.
        /// </value>
        [XmlAttribute(LAT_LABEL), ProtoMember(1), DefaultValue(0)]
        public double Latitude
        {
            get { return latitude; }
            set
            {
                if (Equals(value, latitude)) return;
                latitude = value;
                NotifyOfPropertyChange(() => Latitude);
            }
        }

        /// <summary>
        ///     Gets or sets the longitude in degrees.
        /// </summary>
        /// <value>
        ///     The longitude.
        /// </value>
        [XmlAttribute(LONG_LABEL), ProtoMember(2), DefaultValue(0)]
        public double Longitude
        {
            get { return longitude; }
            set
            {
                if (Equals(value, longitude)) return;
                longitude = value;
                NotifyOfPropertyChange(() => Longitude);
            }
        }

        /// <summary>
        ///     Gets or sets the altitude in meters.
        /// </summary>
        /// <value>
        ///     The altitude.
        /// </value>
        [XmlAttribute(ALT_LABEL), ProtoMember(3), DefaultValue(0)]
        public double Altitude
        {
            get { return altitude; }
            set
            {
                if (Equals(value, altitude)) return;
                altitude = value;
                NotifyOfPropertyChange(() => Altitude);
            }
        }

        /// <summary>
        ///     Absolute speed in the direction of movement.
        /// </summary>
        [XmlAttribute("speed"), ProtoMember(4), DefaultValue(0)]
        public double Speed
        {
            get { return speed; }
            set
            {
                if (Equals(value, speed)) return;
                speed = value;
                NotifyOfPropertyChange(() => Speed);
            }
        }

        /// <summary>
        ///     The course in degrees, where 0 means moving North.
        ///     NOTE: The Orientation is the direction your bow is heading, but the course tells you the direction in which you are
        ///     moving.
        /// </summary>
        [XmlAttribute("course"), ProtoMember(5), DefaultValue(0)]
        public double Course
        {
            
            get { return course; }
            set
            {
                if (Equals(value, course)) return;
                course = value;
                NotifyOfPropertyChange(() => Course);
            }
        }

        /// <summary>
        ///     The elevation in degrees (moving up or down)
        /// </summary>
        [XmlAttribute("elevation"), ProtoMember(6), DefaultValue(0)]
        public double Elevation
        {
            get { return elevation; }
            set
            {
                if (Equals(value, elevation)) return;
                elevation = value;
                NotifyOfPropertyChange(() => Elevation);
            }
        }

        [ProtoMember(7)]
        public double Accuracy
        {
            get { return accuracy; }
            set
            {
                accuracy = value;
                NotifyOfPropertyChange(() => Accuracy);
            }
        }

        public DateTime Date { get; set; }

        public string ToGeoJson()
        {
            return Longitude > 180 || Longitude < -180 || Latitude > 90 || Latitude < -90
                ? "{\"type\":\"Point\",\"coordinates\":[0,0]}"
                : string.Format(CultureInfo.InvariantCulture, "{{\"type\":\"Point\",\"coordinates\":[{0:F6},{1:F6}]}}", Longitude,
                    Latitude);
        }

        public IConvertibleGeoJson FromGeoJson(string geoJson, bool newObject)
        {
            throw new NotImplementedException("Cannot initialize Position object from GeoJSON yet.");
        }

        public IConvertibleGeoJson FromGeoJson(JObject geoJsonObject, bool newObject = true)
        {
            throw new NotImplementedException("Cannot initialize Position object from GeoJSON yet.");
        }

        public string ToCsv(char separator = ',') // Always keep in sync with GetCsvHeader and FromCsv (x2)
        {
            return Latitude .ToString(CultureInfo.InvariantCulture) + separator +
                   Longitude.ToString(CultureInfo.InvariantCulture) + separator +
                   Altitude .ToString(CultureInfo.InvariantCulture) + separator +
                   Speed    .ToString(CultureInfo.InvariantCulture) + separator +
                   Course   .ToString(CultureInfo.InvariantCulture) + separator +
                   Elevation.ToString(CultureInfo.InvariantCulture) + separator + // SdJ added
                   Accuracy .ToString(CultureInfo.InvariantCulture) + separator +
                   Date     .Ticks;
            //return string .Format(CultureInfo.InvariantCulture,"{0:0.00000}, {1:0.00000}, {2:0.000}, {3:0.00}, {4:0.00}, {5:0.0})", Latitude, Longitude, Altitude, Speed, Course, Elevation);        
        }

        // Interface method, assuming a fixed order and no other data in the CSV.
        public IConvertibleCsv FromCsv(string ln, char separator = ',', bool newObject = true)
            // Always keep in sync with GetCsvHeader and ToCsv and other FromCsv
        {
            Position ret = newObject ? new Position() : this;

            var l = ln.Split(separator);
            ret.Latitude  = double.Parse(l[0], CultureInfo.InvariantCulture);
            ret.Longitude = double.Parse(l[1], CultureInfo.InvariantCulture);
            ret.Altitude = double.Parse(l[2], CultureInfo.InvariantCulture);
            ret.Speed = double.Parse(l[3], CultureInfo.InvariantCulture);
            ret.Course = double.Parse(l[4], CultureInfo.InvariantCulture);
            ret.Elevation = double.Parse(l[5], CultureInfo.InvariantCulture);
            ret.Accuracy = double.Parse(l[6], CultureInfo.InvariantCulture);
            ret.Date = new DateTime(long.Parse(l[7]));

            return ret;
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            reader.MoveToContent();
            string attribute = reader.GetAttribute(LAT_LABEL);
            if (!string.IsNullOrEmpty(attribute))
                double.TryParse(attribute, NumberStyles.Any, CultureInfo.InvariantCulture, out latitude);
            attribute = reader.GetAttribute(LONG_LABEL);
            if (!string.IsNullOrEmpty(attribute))
                double.TryParse(attribute, NumberStyles.Any, CultureInfo.InvariantCulture, out longitude);
            attribute = reader.GetAttribute(ALT_LABEL);
            if (!string.IsNullOrEmpty(attribute))
                double.TryParse(attribute, NumberStyles.Any, CultureInfo.InvariantCulture, out altitude);
            attribute = reader.GetAttribute("speed");
            if (!string.IsNullOrEmpty(attribute))
                double.TryParse(attribute, NumberStyles.Any, CultureInfo.InvariantCulture, out speed);
            attribute = reader.GetAttribute("course");
            if (!string.IsNullOrEmpty(attribute))
                double.TryParse(attribute, NumberStyles.Any, CultureInfo.InvariantCulture, out course);
            attribute = reader.GetAttribute("elevation");
            if (!string.IsNullOrEmpty(attribute))
                double.TryParse(attribute, NumberStyles.Any, CultureInfo.InvariantCulture, out elevation);
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString(LAT_LABEL, latitude.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString(LONG_LABEL, longitude.ToString(CultureInfo.InvariantCulture));
            if (altitude < 0 || altitude > 0)
                writer.WriteAttributeString(ALT_LABEL, altitude.ToString(CultureInfo.InvariantCulture));
            if (speed < 0 || speed > 0)
                writer.WriteAttributeString("speed", speed.ToString(CultureInfo.InvariantCulture));
            if (course < 0 || course > 0)
                writer.WriteAttributeString("course", course.ToString(CultureInfo.InvariantCulture));
            if (elevation < 0 || elevation > 0)
                writer.WriteAttributeString("elevation", elevation.ToString(CultureInfo.InvariantCulture));
        }

  

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "Position ({0:0.000}, {1:0.000}, {2:0.0}, {3:0.0}, {4:0.0}, {5:0.0})", Latitude,
                Longitude, Altitude, Speed, Course, Elevation);
        }

        public MapPoint ToMapPoint()
        {
            return new MapPoint(longitude, latitude);
        }

        private static readonly WebMercator WebMercator = new WebMercator();

        public MapPoint ToWebMercator()
        {
            return WebMercator.FromGeographic(ToMapPoint()) as MapPoint;
        }

        /// <summary>
        /// Try not to use this function! Use MapPoint 
        /// </summary>
        /// <returns></returns>
        public System.Windows.Point ToPoint()
        {
            return new System.Windows.Point(Longitude, Latitude);
        }

        /// <summary>
        /// Computes the distance between two positions in WGS84
        /// </summary>
        /// <param name="toPosition"></param>
        /// <returns>The distance in meters in WGS84.</returns>
        public double Distance(Position toPosition)
        {
            //var g = new GeodeticCalculator();
            
            //var gc = new GeodeticCalculator();
            var measurement = GeodeticCalculator.CalculateGeodeticMeasurement(Ellipsoid.WGS84,
                                                                              new GlobalPosition(new GlobalCoordinates(new Angle(Latitude), new Angle(Longitude))),
                                                                              new GlobalPosition(new GlobalCoordinates(new Angle(toPosition.Latitude), new Angle(toPosition.Longitude))));
            return Math.Abs(measurement.EllipsoidalDistance);
        }

                private static double DegreeToRadian(double angle)
        {
            return Math.PI * angle / 180.0;
        }
        private static double RadianToDegree(double angle)
        {
            return angle * (180.0 / Math.PI);
        }

        /// <summary>
        /// Computes a new point based on angle and distance
        /// </summary>
        /// <param name="angle">Angle in degrees</param>
        /// <param name="angle">Distance in KM</param>
        /// <returns>The position.</returns>
        public Position AngleAndDistance(double angle, double distanceKilometres)
        {
            angle = DegreeToRadian(angle);
            const double radiusEarthKilometres = 6371.01;
            var distRatio = distanceKilometres / radiusEarthKilometres;
            var distRatioSine = Math.Sin(distRatio);
            var distRatioCosine = Math.Cos(distRatio);

            var startLatRad = DegreeToRadian(Latitude);
            var startLonRad = DegreeToRadian(Longitude);

            var startLatCos = Math.Cos(startLatRad);
            var startLatSin = Math.Sin(startLatRad);

            var endLatRads = Math.Asin((startLatSin * distRatioCosine) + (startLatCos * distRatioSine * Math.Cos(angle)));

            var endLonRads = startLonRad
                + Math.Atan2(
                    Math.Sin(angle) * distRatioSine * startLatCos,
                    distRatioCosine - startLatSin * Math.Sin(endLatRads));

            return new Position
            {
                Latitude = RadianToDegree(endLatRads),
                Longitude = RadianToDegree(endLonRads)
            };
        }

        /// <summary>
        /// Computes the bearing to an other point
        /// </summary>
        /// <param name="toPosition">Other point</param>
        /// <returns>Angle in degrees.</returns>
        public double Bearing(Position toPosition)
        {
            const double R = 6371; //earth’s radius (mean radius = 6,371km)
            var dLon = DegreeToRadian(toPosition.Longitude - Longitude);
            var dPhi = Math.Log(
                Math.Tan(DegreeToRadian(toPosition.Latitude) / 2 + Math.PI / 4) / Math.Tan(DegreeToRadian(Latitude) / 2 + Math.PI / 4));
            if (Math.Abs(dLon) > Math.PI)
                dLon = dLon > 0 ? -(2 * Math.PI - dLon) : (2 * Math.PI + dLon);
            return ToBearing(Math.Atan2(dLon, dPhi));
        }
        private static double ToBearing(double radians)
        {
            // convert radians to degrees (as bearing: 0...360)
            return (RadianToDegree(radians) + 360) % 360;
        }
    }
}