using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csCommon.Plugins.MgrsGrid
{
    using GeoUtility.GeoSystem;
    using GeoUtility.GeoSystem.Helper;

    using JsonFx.Model;
    using System.Globalization;
    public class WGS84LatLongPoint
    {

        private const int DecimalMinutePrecision = 6;
        private const int DegreesMinuteSecondPrecision = 6;

        public enum TextualDescription
        {
            Decimal,
            DecimalMinutes,
            DMS
        }

        public enum LatLon
        {
            Latitude,
            Longitude
        }

        public static WGS84LatLongPoint Create(double pLatitude, double pLongitude)
        {
            return new WGS84LatLongPoint()
            {
                Latitude = pLatitude,
                Longitude = pLongitude
            };
        }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public UTM ConvertToUtm()
        {
            var geo = new Geographic(Longitude, Latitude, GeoDatum.WGS84);
            return (UTM)geo;
        }

        public MGRS ConvertToMgrs()
        {
            var geo = new Geographic(Longitude, Latitude, GeoDatum.WGS84);
            return (MGRS)geo;
        }

        public string ToTextualDescription(TextualDescription pNotation)
        {
            try
            {
                switch (pNotation)
                {
                    case TextualDescription.Decimal:
                        return String.Format(CultureInfo.InvariantCulture, "WGS84({0:00.000000},{1:000.000000})", Latitude, Longitude);
                    case TextualDescription.DecimalMinutes:
                        return String.Format(CultureInfo.InvariantCulture, "WGS84({0},{1})", ToDecimalMinutes(Latitude, LatLon.Latitude), ToDecimalMinutes(Longitude, LatLon.Longitude));
                    case TextualDescription.DMS:
                        return String.Format(CultureInfo.InvariantCulture, "WGS84({0},{1})", ToDMS(Latitude, LatLon.Latitude), ToDMS(Longitude, LatLon.Longitude));
                }
            }
            catch (Exception lEx)
            {
                return lEx.Message;
            }
            return "-";
        }

        private static char LatLonSign(double pDecimal, LatLon pType)
        {
            char direction = ' ';
            switch (pType)
            {
                case LatLon.Latitude:
                    direction = (pDecimal < 0) ? 'S' : 'N';
                    break;
                case LatLon.Longitude:
                    direction = (pDecimal < 0) ? 'W' : 'E';
                    break;
            }
            return direction;
        }

        private static string ToDecimalMinutes(double pDecimal, LatLon pType)
        {
            var decimalMinutes = Math.Round(((Math.Abs(pDecimal) - Math.Abs((int)pDecimal)) * 60.0), DecimalMinutePrecision);
            if (decimalMinutes >= 60) decimalMinutes = 59.9999;
            char direction = LatLonSign(pDecimal, pType);
            return String.Format(CultureInfo.InvariantCulture, "{0}°{1}'{2}", Math.Abs(pDecimal), decimalMinutes, direction);
        }

        private static string ToDMS(double pDecimal, LatLon pType)
        {
            char direction = LatLonSign(pDecimal, pType);
            double d = (Math.Abs(pDecimal) - Math.Abs((int)pDecimal));
            int minutes = (int)(d * 60.0);
            double seconds = Math.Round((double)(((d * 60.0) - (int)minutes) * 60.0), DegreesMinuteSecondPrecision);
            return String.Format(CultureInfo.InvariantCulture, "{0}°{1}'{2}\"{3}", Math.Abs(pDecimal), minutes, seconds, direction);

        }
    }
}
