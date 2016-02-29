using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GenerateLocationData.Model
{
    public class Location
    {
        public Location(string wkt)
        {
            if (string.IsNullOrEmpty(wkt)) return;
            var centerString = wkt.Substring(6).Split(new[] { ' ', ')' }, 4, StringSplitOptions.RemoveEmptyEntries);
            double longitude;
            if (double.TryParse(centerString[0], NumberStyles.Number, CultureInfo.InvariantCulture, out longitude))
                Longitude = longitude;
            double latitude;
            if (double.TryParse(centerString[1], NumberStyles.Number, CultureInfo.InvariantCulture, out latitude))
                Latitude = latitude;
        }

        public Location(double longitude, double latitude)
        {
            Longitude = longitude;
            Latitude  = latitude;
        }

        public double Longitude { get; set; }
        public double Latitude  { get; set; }
    }

    public class LocationDescription
    {
        public LocationDescription(string id, string name, string center, string rdBoundary, string wgsBoundary)
        {
            Id            = id;
            Name          = name;
            RdBoundary    = rdBoundary;
            Wgs84Boundary = wgsBoundary;
            Center        = new Location(center);
            Features      = new Dictionary<string, string>();
        }

        /// <summary>
        /// Unique ID, i.e. bu_code
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Name of the location
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Center of the location (boundary) in WGS84
        /// </summary>
        public Location Center { get; set; }

        /// <summary>
        /// Boundary of the location area in RD and expressed as WKT.
        /// </summary>
        public string RdBoundary { get; set; }

        /// <summary>
        /// Boundary of the location area in WGS84 and expressed as WKT.
        /// </summary>
        public string Wgs84Boundary { get; set; }

        /// <summary>
        /// Feature set, where each feature is separated by semi-columns.
        /// </summary>
        public Dictionary<string, string> Features { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0}, {1}", Id, Name));
            foreach (var kvp in Features)
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "\t{0}: {1}", kvp.Key, kvp.Value));
            return sb.ToString();
        }
    }
}