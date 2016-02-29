using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Documents;
using csShared.Utils;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Xml.Serialization;

namespace DataServer.SqlProcessing
{
    [Serializable]
    [DebuggerDisplay("Type: {Type}, Label: {LabelName}")]
    public class SqlInputParameter
    {
        ///// <summary>
        ///// Index of the SQL parameter in the query.
        ///// </summary>
        //[XmlAttribute]
        //public int Index { get; set; }
        /// <summary>
        /// Type of parameter.
        /// </summary>
        [XmlAttribute("type")]
        public SqlParameterTypes Type { get; set; }
        /// <summary>
        /// The value of the SQL parameter (is inserted in the query).
        /// </summary>
        [XmlIgnore]
        public string SqlParameterValue { get; set; }
        /// <summary>
        /// Optional (in case the Type == Label), the label name
        /// </summary>
        [XmlAttribute("labelName")]
        public string LabelName { get; set; }

        public bool SetValue(PoI poi, List<Point> zone)
        {
            var isSucces = false;
            switch (Type)
            {
                case SqlParameterTypes.ExtentRD:
                case SqlParameterTypes.RegionRD:
                    if (zone != null)
                    {
                        SqlParameterValue = zone.ConvertPointsToWkt(CoordinateType.Rd);
                        isSucces = true;
                    }
                    break;
                case SqlParameterTypes.PointWGS84:
                    if (poi != null)
                    {
                        SqlParameterValue = poi.Position.Longitude.ToString(CultureInfo.InvariantCulture)+","+poi.Position.Latitude.ToString(CultureInfo.InvariantCulture);
                        isSucces = true;
                    }
                    break;
                case SqlParameterTypes.ShapeWKT:
                    if (poi != null) {
                        if (!string.IsNullOrEmpty(poi.WktText)) {
                            SqlParameterValue = poi.WktText;
                        }
                        else if (poi.Points != null && poi.Points.Count > 3) SqlParameterValue = poi.Points.ToList().ConvertPointsToWkt(CoordinateType.Rd);
                        isSucces = true;
                    }
                    break;
                case SqlParameterTypes.ShapeEWKT:
                    if (poi != null) {
                        if (!string.IsNullOrEmpty(poi.WktText)) {
                            SqlParameterValue = poi.WktText;
                        }
                        else if (poi.Points != null && poi.Points.Count > 3) SqlParameterValue = poi.Points.ToList().ConvertPointsToEwkt();
                        isSucces = true;
                    }
                    break;
                case SqlParameterTypes.PointRD:
                    if (poi != null)
                    {
                        var coordinate = CoordinateUtils.LatLon2Rd(new Point(poi.Position.Longitude, poi.Position.Latitude));
                        SqlParameterValue = coordinate.X.ToString(CultureInfo.InvariantCulture) + "," + coordinate.Y.ToString(CultureInfo.InvariantCulture);
                        isSucces = true;
                    }
                    break;
                case SqlParameterTypes.ExtentWGS84:
                case SqlParameterTypes.RegionWGS84:
                    if (zone != null)
                    {
                        SqlParameterValue = zone.ConvertPointsToWkt(CoordinateType.Degrees);
                        isSucces = true;
                    }
                    break;
                case SqlParameterTypes.RadiusInMeter:
                case SqlParameterTypes.Label:
                    if (poi == null) break;
                    if (poi.Labels.ContainsKey(LabelName))
                    {
                        SqlParameterValue = poi.Labels[LabelName];
                        isSucces = true;
                    }
                    else if (poi.Service != null && poi.Service.Settings != null && poi.Service.Settings.Labels.ContainsKey(LabelName))
                    {
                        SqlParameterValue = poi.Service.Settings.Labels[LabelName];
                        isSucces = true;
                    }
                    break;
            }
            return isSucces;
        }
    }
}


//case SqlParameterTypes.PointWGS84:
//                    if (zone != null)
//                    {
//                        SqlParameterValue = poi.Position.Longitude.ToString(CultureInfo.InvariantCulture)+","+poi.Position.Latitude.ToString(CultureInfo.InvariantCulture);
//                        isSucces = true;
//                    }
//                    break;
//                case SqlParameterTypes.PointRD:
//                    if (zone != null)
//                    {
//                        var coordinate = CoordinateUtils.LatLon2Rd(new Point(poi.Position.Longitude, poi.Position.Latitude));
//                        SqlParameterValue = coordinate.X.ToString(CultureInfo.InvariantCulture) + "," + coordinate.Y.ToString(CultureInfo.InvariantCulture);
//                        isSucces = true;
//                    }
//                    break;