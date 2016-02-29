#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using DataServer;
using DocumentFormat.OpenXml.Office.CoverPageProps;
using SharpMap.Geometries;
using Point = System.Windows.Point;

#endregion

namespace csShared.Utils
{
    public static class CoordinateUtils
    {

        [DllImport("proj.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr pj_init_plus(string init);
        [DllImport("proj.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void pj_free(IntPtr pointer);
        [DllImport("proj.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pj_transform(IntPtr srcPrj, IntPtr destPrj, int nPoints, int offset, ref double x, ref double y, ref double z);
        [DllImport("proj.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pj_is_latlong(IntPtr coordPointer);

        #region CoordinateDirection enum

        public enum CoordinateDirection
        {
            NorthSouth,
            WestEast
        };

        #endregion

        /// <summary>
        ///  Formats the coordinate according to current setting.
        /// </summary>
        /// <param name = "pCoordinateType">Type of the coordinate.</param>
        /// <param name = "pLatitude">The latitude in degrees.</param>
        /// <param name = "pLongitude">The longitude in degrees.</param>
        /// <param name = "pWestEastValue">The west east value in the specified format.</param>
        /// <param name = "pNorthSouthValue">The north south value in the specified format.</param>
        public static void CoordinateToString
            (CoordinateType pCoordinateType,
                double pLatitude,
                double pLongitude,
                out string pWestEastValue,
                out string pNorthSouthValue)
        {
            switch (pCoordinateType)
            {
                // Latitude-longitude.
                case CoordinateType.Degrees:
                    pWestEastValue = string.Format("{0:0.00000}", pLongitude);
                    pNorthSouthValue = string.Format("{0:0.00000}", pLatitude);
                    break;
                case CoordinateType.Degreeminutesecond:
                    pWestEastValue = DecimalDegreestoDegreeMinuteSecond(pLongitude, CoordinateDirection.WestEast);
                    pNorthSouthValue = DecimalDegreestoDegreeMinuteSecond(pLatitude, CoordinateDirection.NorthSouth);
                    break;
                case CoordinateType.Xy:
                    var point = LatLonDegreesToXYPoint(pLatitude, pLongitude);
                    pWestEastValue = string.Format("{0:0.00000}", point.X);
                    pNorthSouthValue = string.Format("{0:0.00000}", point.Y);
                    break;
                // Rijksdriehoek.
                case CoordinateType.Rd:
                    double x;
                    double y;
                    LonLat2Rd(pLongitude, pLatitude, out x, out y);
                    pWestEastValue = string.Format("{0:0}", x);
                    pNorthSouthValue = string.Format("{0:0}", y);
                    break;
                default:
                    pWestEastValue = string.Format("{0:0.00000}", pLongitude);
                    pNorthSouthValue = string.Format("{0:0.00000}", pLatitude);
                    break;
            }
        }

        /// <summary>
        ///  Retrieves latitude and longitude in degrees from the string values presented in the given coordinate type.
        /// </summary>
        /// <param name = "pCoordinateType">Type of the coordinate.</param>
        /// <param name = "pWestEastValue">The west east value.</param>
        /// <param name = "pNorthSouthValue">The north south value.</param>
        /// <param name = "pLatitude">The latitude in degrees.</param>
        /// <param name = "pLongitude">The longitude in degrees.</param>
        public static void FormattedCoordinateToDouble
            (CoordinateType pCoordinateType,
                string pWestEastValue,
                string pNorthSouthValue,
                out double pLatitude,
                out double pLongitude)
        {
            {
                double x;
                double y;
                switch (pCoordinateType)
                {
                    case CoordinateType.Degrees:
                        if (double.TryParse(pWestEastValue, out pLongitude) &&
                            double.TryParse(pNorthSouthValue, out pLatitude)) break;
                        throw new ArgumentException("Incorrect coordinate format");
                    case CoordinateType.Degreeminutesecond:
                        pLatitude = GetCoordinateDegreesFromDegreeMinuteSecond(pNorthSouthValue);
                        pLongitude = GetCoordinateDegreesFromDegreeMinuteSecond(pWestEastValue);
                        break;
                    case CoordinateType.Xy:
                        if (double.TryParse(pWestEastValue, out x) && double.TryParse(pNorthSouthValue, out y))
                        {
                            XYPointToLatLonDegrees(x, y, out pLatitude, out pLongitude);
                            break;
                        }
                        throw new ArgumentException("Incorrect coordinate format");
                    // Rijksdriehoek.
                    case CoordinateType.Rd:
                        if (double.TryParse(pWestEastValue, out x) && double.TryParse(pNorthSouthValue, out y))
                        {
                            Rd2LonLat(x, y, out pLongitude, out pLatitude);
                            break;
                        }
                        throw new ArgumentException("Incorrect coordinate format");
                    default:
                        if (double.TryParse(pWestEastValue, out pLongitude) &&
                            double.TryParse(pNorthSouthValue, out pLatitude)) break;
                        throw new ArgumentException("Incorrect coordinate format");
                }
            }
        }

        //4326 //3035
        public static List<Point> TransformCoordinates(List<Point> points, int sourceProjection, int destProjection)
        {
            var returnpoints = new List<Point>();
            var _srcPrj = pj_init_plus("+proj=longlat +ellps=WGS84 +datum=WGS84 +no_defs");
            var _destPrj =
                pj_init_plus("+proj=laea +lat_0=52 +lon_0=10 +x_0=4321000 +y_0=3210000 +ellps=GRS80 +units=m +no_defs");
            foreach (var p in points)
            {
                var x = p.X * 0.0174532925199433;
                var y = p.Y * 0.0174532925199433;
                var z = 0.0;
                pj_transform(_srcPrj, _destPrj, 1, 0, ref x, ref y, ref z);
                returnpoints.Add(new Point(x, y));
            }
            pj_free(_srcPrj);
            pj_free(_destPrj);

            return returnpoints;
        }


        public static List<Point> TransformCoordinates(List<Point> points, string sourceProjection, string destProjection)
        {
            var returnpoints = new List<Point>();
            var _srcPrj = pj_init_plus(sourceProjection); //for example "+proj=longlat +ellps=WGS84 +datum=WGS84 +no_defs"
            var _destPrj = pj_init_plus(destProjection); // for example "+proj=laea +lat_0=52 +lon_0=10 +x_0=4321000 +y_0=3210000 +ellps=GRS80 +units=m +no_defs"
            foreach (var p in points)
            {
                var x = p.X;
                var y = p.Y;
                var z = 0.0;
                pj_transform(_srcPrj, _destPrj, 1, 0, ref x, ref y, ref z);
                returnpoints.Add(new Point(x, y));
            }
            pj_free(_srcPrj);
            pj_free(_destPrj);

            return returnpoints;
        }

        /// <summary>
        ///  Convert a lat/lon point to a RD point.
        /// </summary>
        /// <param name = "pLatLon">The lat lon point. (x=latitude, y=longitude and in radian)</param>
        /// <returns>The RD point</returns>
        public static Point LatLon2Rd(Point pLatLon)
        {
            double x, y;
            LonLat2Rd(Rad2Deg(pLatLon.Y), Rad2Deg(pLatLon.X), out x, out y);

            return new Point(x, y);
        }

        /// <summary>
        ///  Convert a lat/lon point to a RD point.
        /// </summary>
        /// <param name = "pLatLon">The lat lon point. (x=latitude, y=longitude and in degrees)</param>
        /// <returns>The RD point</returns>
        public static Point LatLonDeg2Rd(Point pLatLon)
        {
            double x, y;
            LonLat2Rd(pLatLon.Y, pLatLon.X, out x, out y);

            return new Point(x, y);
        }

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //:: This function converts decimal degrees to radians       :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        public static double Deg2Rad(double pDegrees) { return (pDegrees * Math.PI / 180.0); }

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //:: This function converts radians to decimal degrees       :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        public static double Rad2Deg(double pRadians) { return (pRadians / Math.PI * 180.0); }



        public static double AreaLatLon(List<Point> Points)
        {
            var area = 0.0;

            var points = TransformCoordinates(Points, 4326, 3035);
            area = Area(points);

            var txt = "polygon((";
            foreach (var p in Points)
            {
                txt += p.X.ToString(CultureInfo.InvariantCulture) + " " + p.Y.ToString(CultureInfo.InvariantCulture) + ", ";
            }
            txt += "))";
            return area;

        }

        public static double Area(List<Point> Points)
        {
            var area = 0.0;

            if (Points.Count > 1)
            {
                if (Points.Last() != Points.First())
                    Points.Add(Points.First());
                //if (Points.Last() != Points.First())
                //    Points.Add(Points.First());

                //for (var i = 0; i < Points.Count; i++)
                //{
                //    var j = (i + 1) % Points.Count;

                //    area += Points[i].X * Points[j].Y;
                //    area -= Points[i].Y * Points[j].X;
                //}

                //area /= 2;
                area = System.Math.Abs(Points.Take(Points.Count - 1)
                                        .Select((p, i) => (Points[i + 1].X - p.X) * (Points[i + 1].Y + p.Y))
                                        .Sum() / 2);
            }
            var txt = "polygon((";
            foreach (var p in Points)
            {
                txt += p.X.ToString(CultureInfo.InvariantCulture) + " " + p.Y.ToString(CultureInfo.InvariantCulture) + ", \r\n";
            }
            txt += "))";
            return area / 1000000;

        }


        /// <summary>
        ///  Calculates the distance between the specified point1 (lat1, lon1) and point2 (lat2, lon2).
        /// </summary>
        /// <param name = "pLat1">The lat1.</param>
        /// <param name = "pLon1">The lon1.</param>
        /// <param name = "pLat2">The lat2.</param>
        /// <param name = "pLon2">The lon2.</param>
        /// <param name = "pUnit">The unit (K for kilometers (default), N for nautical miles, or M for miles (default)).</param>
        /// <returns></returns>
        public static double Distance(double pLat1, double pLon1, double pLat2, double pLon2, char pUnit = 'K')
        {
            var theta = pLon1 - pLon2;
            var dist = Math.Sin(Deg2Rad(pLat1)) * Math.Sin(Deg2Rad(pLat2)) + Math.Cos(Deg2Rad(pLat1)) * Math.Cos(Deg2Rad(pLat2)) * Math.Cos(Deg2Rad(theta));
            dist = Math.Acos(dist);
            dist = Rad2Deg(dist);
            dist = dist * 60 * 1.1515;
            if (pUnit == 'K') dist = dist * 1.609344;
            else if (pUnit == 'N') dist = dist * 0.8684;
            if (double.IsNaN(dist))
                dist = 0;
            return (dist);
        }

        /// <summary>
        ///  Calculates the distance between the specified point1 and point2.
        /// </summary>
        /// <param name = "pPoint1">The point1.</param>
        /// <param name = "pPoint2">The point2.</param>
        /// <returns></returns>
        public static double Distance(Point pPoint1, Point pPoint2) { return Math.Sqrt(Math.Abs(pPoint1.X - pPoint2.X) + Math.Abs(pPoint1.Y - pPoint2.Y)); }

        #region Coordinate conversions

        #region Constants

        public const double EarthRadius = 6378137;
        private const double DegreesToRadians = Math.PI / 180;
        private const double HalfPi = Math.PI / 2;

        private const double Labda0 = 5.38720621;
        private const double Phi0 = 52.15517440;

        private const double R01 = 190094.945;
        private const double R02 = -0.008;
        private const double R03 = -32.391;
        private const double R10 = -0.705;
        private const double R11 = -11832.228;
        private const double R13 = -0.608;
        private const double R21 = -114.221;
        private const double R23 = 0.148;
        private const double R31 = -2.340;

        private const double S01 = 0.433;
        private const double S02 = 3638.893;
        private const double S04 = 0.092;
        private const double S10 = 309056.544;
        private const double S11 = -0.032;
        private const double S12 = -157.984;
        private const double S14 = -0.054;
        private const double S20 = 73.077;
        private const double S22 = -6.439;
        private const double S30 = 59.788;
        private const double X0 = 155000;
        private const double Y0 = 463000;

        private const double K01 = 3235.65389;
        private const double K02 = -0.24750;
        private const double K03 = -0.06550;
        private const double K10 = -0.00738;
        private const double K11 = -0.00012;
        private const double K20 = -32.58297;
        private const double K21 = -0.84978;
        private const double K22 = -0.01709;
        private const double K23 = -0.00039;
        private const double K40 = 0.00530;
        private const double K41 = 0.00033;
        private const double L01 = 0.01199;
        private const double L02 = 0.00022;
        private const double L10 = 5260.52916;
        private const double L11 = 105.94684;
        private const double L12 = 2.45656;
        private const double L13 = 0.05594;
        private const double L14 = 0.00128;
        private const double L20 = -0.00022;
        private const double L30 = -0.81885;
        private const double L31 = -0.05607;
        private const double L32 = -0.00256;
        private const double L50 = 0.00026;

        #endregion

        ///<summary>
        /// Convert a string such as 039°32'09,90 to a double
        /// 
        /// A description of the regular expression:
        /// 
        /// 0, zero or one repetitions
        /// [Degrees]: A named capture group. [\d{1,3}]
        /// Any digit, between 1 and 3 repetitions
        /// °0?
        /// °0, zero or one repetitions
        /// [Minutes]: A named capture group. [\d{1,2}]
        /// Any digit, between 1 and 2 repetitions
        /// '0?
        /// '0, zero or one repetitions
        /// [Seconds]: A named capture group. [\d{1,2}[.,]?\d*]
        /// \d{1,2}[.,]?\d*
        /// Any digit, between 1 and 2 repetitions
        /// Any character in this class: [.,], zero or one repetitions
        /// Any digit, any number of repetitions
        ///</summary>
        public static Regex MConvertLatLongStringToDouble =
          new Regex("0?(?<Degrees>\\d{1,3})°0?(?<Minutes>\\d{1,2})'0?(?<Seconds>\\d{1,2}[.,]?\\d*)",
               RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
        ///  Converts the XYPoint to latitude longitude in degrees.
        /// </summary>
        /// <param name = "pX">The X.</param>
        /// <param name = "pY">The Y.</param>
        /// <param name = "pLatitude">The resulting latitude in degrees.</param>
        /// <param name = "pLongitude">The resulting longitude in degrees.</param>
        public static void XYPointToLatLonDegrees(double pX, double pY, out double pLatitude, out double pLongitude)
        {
            double latitudeRad;
            double longitudeRad;

            XYPointToLatLonRad(pX, pY, out latitudeRad, out longitudeRad);

            pLatitude = (latitudeRad / DegreesToRadians);
            pLongitude = (longitudeRad / DegreesToRadians);
        }

        /// <summary>
        /// Convert RD (rijksdriehoek) to WGS84.
        /// </summary>
        /// <param name="x">X in RD</param>
        /// <param name="y">Y in RD</param>
        /// <param name="longitude">Longitude in WGS84</param>
        /// <param name="latitude">Latitude in WGS84</param>
        /// <returns></returns>
        /// <see cref=" http://www.dekoepel.nl/pdf/Transformatieformules.pdf" />
        /// <seealso cref="http://www.roelvanlisdonk.nl/?p=2950"/>
        public static void ConvertRDToLonLat(double x, double y, out double longitude, out double latitude)
        {
            // The city "Amsterfoort" is used as reference "Rijksdriehoek" coordinate.
            const int referenceRdX = 155000;
            const int referenceRdY = 463000;

            var dX = (x - referenceRdX) * Math.Pow(10, -5);
            var dY = (y - referenceRdY) * Math.Pow(10, -5);

            var sumN =
                (3235.65389 * dY) +
                (-32.58297 * Math.Pow(dX, 2)) +
                (-0.2475 * Math.Pow(dY, 2)) +
                (-0.84978 * Math.Pow(dX, 2) * dY) +
                (-0.0655 * Math.Pow(dY, 3)) +
                (-0.01709 * Math.Pow(dX, 2) * Math.Pow(dY, 2)) +
                (-0.00738 * dX) +
                (0.0053 * Math.Pow(dX, 4)) +
                (-0.00039 * Math.Pow(dX, 2) * Math.Pow(dY, 3)) +
                (0.00033 * Math.Pow(dX, 4) * dY) +
                (-0.00012 * dX * dY);
            var sumE =
                (5260.52916 * dX) +
                (105.94684 * dX * dY) +
                (2.45656 * dX * Math.Pow(dY, 2)) +
                (-0.81885 * Math.Pow(dX, 3)) +
                (0.05594 * dX * Math.Pow(dY, 3)) +
                (-0.05607 * Math.Pow(dX, 3) * dY) +
                (0.01199 * dY) +
                (-0.00256 * Math.Pow(dX, 3) * Math.Pow(dY, 2)) +
                (0.00128 * dX * Math.Pow(dY, 4)) +
                (0.00022 * Math.Pow(dY, 2)) +
                (-0.00022 * Math.Pow(dX, 2)) +
                (0.00026 * Math.Pow(dX, 5));

            // The city "Amsterfoort" is used as reference "WGS84" coordinate.
            const double referenceWgs84X = 52.15517;
            const double referenceWgs84Y = 5.387206;

            latitude = referenceWgs84X + (sumN / 3600);
            longitude = referenceWgs84Y + (sumE / 3600);

            //// Input
            //// x = 122202
            //// y = 487250
            ////
            //// Result
            //// "52.372143838117, 4.90559760435224"
            //result = string.Format("{0}, {1}",
            //    latitude.ToString(CultureInfo.InvariantCulture.NumberFormat),
            //    longitude.ToString(CultureInfo.InvariantCulture.NumberFormat));

            //return result;
        }

        /// <summary>
        ///  Converts the XYPoint to latitude longitude in radians.
        /// </summary>
        /// <param name = "pX">The X.</param>
        /// <param name = "pY">The Y.</param>
        /// <param name = "pLatitude">The resulting latitude.</param>
        /// <param name = "pLongitude">The resulting longitude.</param>
        /// <returns></returns>
        public static void XYPointToLatLonRad(double pX, double pY, out double pLatitude, out double pLongitude)
        {
            var ts = Math.Exp(-pY / (EarthRadius));
            var latRadians = HalfPi - 2 * Math.Atan(ts);

            var lonRadians = pX / (EarthRadius);

            pLatitude = latRadians;
            pLongitude = lonRadians;
        }

        /// <summary>
        ///  Converts the latitude and longitude in radians to XYPoint.
        /// </summary>
        /// <param name = "pLatitude">The lat.</param>
        /// <param name = "pLongitude">The lon.</param>
        /// <returns></returns>
        public static Point LatLonRadToXYPoint(double pLatitude, double pLongitude)
        {
            var x = EarthRadius * pLongitude;
            var y = EarthRadius * Math.Log(Math.Tan(Math.PI * 0.25 + pLatitude * 0.5));

            return new Point((float)x, (float)y);
        }

        /// <summary>
        ///  Converts the latitude longitude in degrees to XY point.
        /// </summary>
        /// <param name = "pLatitude">The p lat.</param>
        /// <param name = "pLongitude">The p lon.</param>
        /// <returns></returns>
        public static Point LatLonDegreesToXYPoint(double pLatitude, double pLongitude) { return LatLonRadToXYPoint(DegreesToRadians * pLatitude, DegreesToRadians * pLongitude); }

        /// <summary>
        ///  Gets the latitude and the longitude in degrees from a latitude longitude string.
        /// </summary>
        /// <param name = "pLatitude">The latitude string.</param>
        /// <param name = "pLongitude">The longitude string.</param>
        /// <returns></returns>
        public static Point LatitudeLongitudeDegreesFromLatitudeLongitudeString(string pLatitude, string pLongitude)
        {
            var lat = 0D;
            var lon = 0D;
            try
            {
                // pLat, long already in degrees
                var result = double.TryParse(pLatitude, NumberStyles.Number, CultureInfo.InvariantCulture, out lon);
                if (result) double.TryParse(pLongitude, NumberStyles.Number, CultureInfo.InvariantCulture, out lat);
                else
                {
                    // or as deg, min, sec
                    lat = GetCoordinateDegreesFromDegreeMinuteSecond(pLatitude);
                    lon = GetCoordinateDegreesFromDegreeMinuteSecond(pLongitude);
                }
            }
            catch (Exception)
            { }

            return new Point(lat, lon);
        }

        /// <summary>
        ///  Gets the coordinate degrees from degree minute second.
        /// </summary>
        /// <param name = "pValue">The value.</param>
        /// <returns></returns>
        private static double GetCoordinateDegreesFromDegreeMinuteSecond(string pValue)
        {
            double result;
            var match = MConvertLatLongStringToDouble.Match(pValue);
            if (match.Success)
                result = double.Parse(match.Groups["Degrees"].Value, NumberStyles.Number, CultureInfo.InvariantCulture) +
                     double.Parse(match.Groups["Minutes"].Value, NumberStyles.Number, CultureInfo.InvariantCulture) / 60 +
                     double.Parse(match.Groups["Seconds"].Value, NumberStyles.Number, CultureInfo.InvariantCulture) / 3600;
            else throw new ArgumentException("Incorrect coordinate format");
            return result;
        }

        /// <summary>
        ///  Convert longitude/latitude to 'Rijksdriehoek' coordinates.
        /// </summary>
        /// <param name = "pLongitude">The longitude in degrees.</param>
        /// <param name = "pLatitude">The latitude in degrees.</param>
        /// <param name = "pRdX">The resulting 'Rijksdriehoek' X.</param>
        /// <param name = "pRdY">The resulting 'Rijksdriehoek' Y.</param>
        public static void LonLat2Rd(double pLongitude, double pLatitude, out double pRdX, out double pRdY)
        {
            var dphi = 0.36 * (pLatitude - Phi0);
            var dlabda = 0.36 * (pLongitude - Labda0);

            pRdX = X0 + R01 * dlabda + R11 * dphi * dlabda + R21 * Math.Pow(dphi, 2) * dlabda + R03 * Math.Pow(dlabda, 3) + R10 * dphi +
                R31 * Math.Pow(dphi, 3) * dlabda + R13 * dphi * Math.Pow(dlabda, 3) + R02 * Math.Pow(dlabda, 2) +
                R23 * Math.Pow(dphi, 2) * Math.Pow(dlabda, 3);
            pRdY = Y0 + S10 * dphi + S02 * Math.Pow(dlabda, 2) + S20 * Math.Pow(dphi, 2) + S12 * dphi * Math.Pow(dlabda, 2) +
                S30 * Math.Pow(dphi, 3) + S01 * dlabda + S22 * Math.Pow(dphi, 2) * Math.Pow(dlabda, 2) + S11 * dphi * dlabda +
                S04 * Math.Pow(dlabda, 4) + S14 * dphi * Math.Pow(dlabda, 4);

            //      Log.Debug("LonLat->RD: " + pLongitude + ", " + pLatitude + " : " + pRdX + ", " + pRdY);
        }


        /// <summary>
        ///  Convert 'Rijksdriehoek' coordinates to longitude/latitude in degrees.
        /// </summary>
        /// <param name = "pRdX">The 'Rijksdriehoek' X.</param>
        /// <param name = "pRdY">The 'Rijksdriehoek' Y.</param>
        /// <returns>System.Windows.Point(lon,lat)</returns>
        public static Point Rd2LonLatAsPoint(double pRdX, double pRdY)
        {
            double lat, lon;
            Rd2LonLat(pRdX, pRdY, out lon, out lat);
            return new Point(lon, lat);
        }

        /// <summary>
        ///  Convert 'Rijksdriehoek' coordinates to longitude/latitude in degrees.
        /// </summary>
        /// <param name = "pRdX">The 'Rijksdriehoek' X.</param>
        /// <param name = "pRdY">The 'Rijksdriehoek' Y.</param>
        public static Position Rd2LonLat(double pRdX, double pRdY)
        {
            double lat, lon;
            Rd2LonLat(pRdX, pRdY, out lon, out lat);
            return new Position(lon, lat);
        }

        /// <summary>
        ///  Convert 'Rijksdriehoek' coordinates to longitude/latitude in degrees.
        /// </summary>
        /// <param name = "pRdX">The 'Rijksdriehoek' X.</param>
        /// <param name = "pRdY">The 'Rijksdriehoek' Y.</param>
        /// <param name = "pLongitude">The resulting longitude in degrees.</param>
        /// <param name = "pLatitude">The resulting latitude in degrees.</param>
        public static void Rd2LonLat(double pRdX, double pRdY, out double pLongitude, out double pLatitude)
        {
            var dX = (pRdX - X0) / 100000;
            var dY = (pRdY - Y0) / 100000;

            pLatitude = Phi0 +
                  (K01 * dY + K20 * Math.Pow(dX, 2) + K02 * Math.Pow(dY, 2) + K21 * Math.Pow(dX, 2) * dY + K03 * Math.Pow(dY, 3) +
                   K22 * Math.Pow(dX, 2) * Math.Pow(dY, 2) + K10 * dX + K40 * Math.Pow(dX, 4) +
                   K23 * Math.Pow(dX, 2) * Math.Pow(dY, 3) + K41 * Math.Pow(dX, 4) * dY + K11 * dX * dY) / 3600;
            pLongitude = Labda0 +
                   (L10 * dX + L11 * dX * dY + L12 * dX * Math.Pow(dY, 2) + L30 * Math.Pow(dX, 3) + L13 * dX * Math.Pow(dY, 3) +
                   L31 * Math.Pow(dX, 3) * dY + L01 * dY + L32 * Math.Pow(dX, 3) + Math.Pow(dY, 2) + L14 * dX * Math.Pow(dY, 4) +
                   L02 * Math.Pow(dY, 2) + L20 * Math.Pow(dX, 2) + L50 * Math.Pow(dX, 5)) / 3600;

            //      Log.Debug("RD->LonLat: " + pRdX + ", " + pRdY + " : " + pLongitude + ", " + pLatitude);
        }


        public static void Rd2LonLat2(double pRdX, double pRdY, out double pLongitude, out double pLatitude)
        {
            pLatitude = RD2lat(pRdX, pRdY);
            pLongitude = RD2lng(pRdX, pRdY);
        }

        public static double RD2lat(double b, double c)
        {
            var latpqK = new pqK[12];
            for (int i = 0; i < latpqK.Length; i++)
                latpqK[i] = new pqK();
            latpqK[1].p = 0;
            latpqK[1].q = 1;
            latpqK[1].K = 3235.65389;
            latpqK[2].p = 2;
            latpqK[2].q = 0;
            latpqK[2].K = -32.58297;
            latpqK[3].p = 0;
            latpqK[3].q = 2;
            latpqK[3].K = -0.2475;
            latpqK[4].p = 2;
            latpqK[4].q = 1;
            latpqK[4].K = -0.84978;
            latpqK[5].p = 0;
            latpqK[5].q = 3;
            latpqK[5].K = -0.0665;
            latpqK[6].p = 2;
            latpqK[6].q = 2;
            latpqK[6].K = -0.01709;
            latpqK[7].p = 1;
            latpqK[7].q = 0;
            latpqK[7].K = -0.00738;
            latpqK[8].p = 4;
            latpqK[8].q = 0;
            latpqK[8].K = 0.0053;
            latpqK[9].p = 2;
            latpqK[9].q = 3;
            latpqK[9].K = -3.9E-4;
            latpqK[10].p = 4;
            latpqK[10].q = 1;
            latpqK[10].K = 3.3E-4;
            latpqK[11].p = 1;
            latpqK[11].q = 1;
            latpqK[11].K = -1.2E-4;

            const double lat0 = 52.1551744;
            var a = 0.0;
            var dX = 1E-5 * (b - X0);
            var dY = 1E-5 * (c - Y0);
            for (var i = 1; 12 > i; i++)
                a += latpqK[i].K * Math.Pow(dX, latpqK[i].p) * Math.Pow(dY, latpqK[i].q);
            return lat0 + a / 3600.0;
        }

        public class pqK
        {
            public int p;
            public int q;
            public double K;
        }

        public static double RD2lng(double b, double c)
        {
            var lngpqL = new pqK[13];
            for (var i = 0; i < lngpqL.Length; i++)
                lngpqL[i] = new pqK();
            lngpqL[1].p = 1;
            lngpqL[1].q = 0;
            lngpqL[1].K = 5260.52916;
            lngpqL[2].p = 1;
            lngpqL[2].q = 1;
            lngpqL[2].K = 105.94684;
            lngpqL[3].p = 1;
            lngpqL[3].q = 2;
            lngpqL[3].K = 2.45656;
            lngpqL[4].p = 3;
            lngpqL[4].q = 0;
            lngpqL[4].K = -0.81885;
            lngpqL[5].p = 1;
            lngpqL[5].q = 3;
            lngpqL[5].K = 0.05594;
            lngpqL[6].p = 3;
            lngpqL[6].q = 1;
            lngpqL[6].K = -0.05607;
            lngpqL[7].p = 0;
            lngpqL[7].q = 1;
            lngpqL[7].K = 0.01199;
            lngpqL[8].p = 3;
            lngpqL[8].q = 2;
            lngpqL[8].K = -0.00256;
            lngpqL[9].p = 1;
            lngpqL[9].q = 4;
            lngpqL[9].K = 0.00128;
            lngpqL[10].p = 0;
            lngpqL[10].q = 2;
            lngpqL[10].K = 2.2E-4;
            lngpqL[11].p = 2;
            lngpqL[11].q = 0;
            lngpqL[11].K = -2.2E-4;
            lngpqL[12].p = 5;
            lngpqL[12].q = 0;
            lngpqL[12].K = 2.6E-4;

            const double lng0 = 5.38720621;
            var a = 0.0;
            var dX = 1E-5 * (b - X0);
            var dY = 1E-5 * (c - Y0);
            for (var i = 1; 13 > i; i++)
                a += lngpqL[i].K * Math.Pow(dX, lngpqL[i].p) * Math.Pow(dY, lngpqL[i].q);
            return lng0 + a / 3600;
        }


        /// <summary>
        ///  Convert decimal degrees (DD) to degree, minute, second (DMS) notation.
        /// </summary>
        /// <param name = "pCoordinate">The Coordinate.</param>
        /// <param name = "pCoordinateDirection">The direction.</param>
        /// <returns></returns>
        public static string DecimalDegreestoDegreeMinuteSecond(double pCoordinate, CoordinateDirection pCoordinateDirection)
        {
            // Set flag if number is negative
            var neg = pCoordinate < 0d;

            // Work with a positive number
            pCoordinate = Math.Abs(pCoordinate);

            // Get d/m/s components
            var d = Math.Floor(pCoordinate);
            pCoordinate -= d;
            pCoordinate *= 60;
            var m = Math.Floor(pCoordinate);
            pCoordinate -= m;
            pCoordinate *= 60;
            var s = Math.Round(pCoordinate);

            // Create padding character
            char pad;
            char.TryParse("0", out pad);

            // Create d/m/s strings
            var dd = d.ToString();
            var mm = m.ToString().PadLeft(2, pad);
            var ss = s.ToString().PadLeft(2, pad);

            // Append d/m/s
            var dms = string.Format("{0}°{1}'{2}\"", dd, mm, ss);

            // Append compass heading
            switch (pCoordinateDirection)
            {
                case CoordinateDirection.WestEast:
                    dms += neg ? "W" : "E";
                    break;
                case CoordinateDirection.NorthSouth:
                    dms += neg ? "S" : "N";
                    break;
            }

            // Return formated string
            return dms;
        }



        /// <summary>
        /// Translates a position, spherical implementation
        /// The position in translated in the azimuth direction of the angle x, y and by a distance of sqrt(x^2, Y^2).
        /// </summary>
        /// <param name="pLatitudeDeg">Latitude of start coordinate in degrees.</param>
        /// <param name="pLongitudeDeg">Longitude of start coordinate in degrees.</param>
        /// <param name="pDistance">The distance to move in meters.</param>
        /// <param name="pAngleDeg">The angle to move under in degrees.</param>
        /// <param name="pNewLatitudeDegrees">The p new latitude degrees.</param>
        /// <param name="pNewLongitudeDegrees">The p new longitude degrees.</param>
        public static void CalculatePointSphere2D(double pLatitudeDeg, double pLongitudeDeg, double pDistance, double pAngleDeg, out double pNewLatitudeDegrees, out double pNewLongitudeDegrees)
        {
            var distanceRad = pDistance / EarthRadius;
            var azimuthRad = Deg2Rad(pAngleDeg);
            var latRad = Deg2Rad(pLatitudeDeg);
            var lonRad = Deg2Rad(pLongitudeDeg);

            var sinDist = Math.Sin(distanceRad);
            var cosDist = Math.Cos(distanceRad);
            var sinLatRad = Math.Sin(latRad);
            var cosLatRad = Math.Cos(latRad);

            var newLat = Math.Asin(sinLatRad * cosDist + cosLatRad * sinDist * Math.Cos(azimuthRad));
            var newLon = lonRad + Math.Atan2(Math.Sin(azimuthRad) * sinDist * cosLatRad, cosDist - sinLatRad * Math.Sin(newLat));

            pNewLongitudeDegrees = Rad2Deg(newLon);
            pNewLatitudeDegrees = Rad2Deg(newLat);
        }

        #endregion

        #region Coordinate Checks

        /// <summary>
        ///  Checks the range of the coordinate and returns the error message if not OK.
        /// </summary>
        /// <param name = "pCoordinateType">Type of the coordinate.</param>
        /// <param name = "pCoordinateDirection">The coordinate direction.</param>
        /// <param name = "pValue">The value.</param>
        /// <returns>Empty string if witin range and a message otherwise</returns>
        public static string CheckCoordinate(CoordinateType pCoordinateType, CoordinateDirection pCoordinateDirection, string pValue)
        {
            double value;
            switch (pCoordinateType)
            {
                // Latitude-longitude.
                case CoordinateType.Degrees:
                    if (double.TryParse(pValue, out value))
                        switch (pCoordinateDirection)
                        {
                            case CoordinateDirection.NorthSouth:
                                if (value >= -90.0 && value <= 90.0) return string.Empty;
                                return CoordinateTypeUtils.GetCoodinateOutOfRangeMessage(pCoordinateType,
                                                             pCoordinateDirection, "-90.0",
                                                             "90.0");
                            case CoordinateDirection.WestEast:
                                if (value >= -180.0 && value <= 180.0) return string.Empty;
                                return CoordinateTypeUtils.GetCoodinateOutOfRangeMessage(pCoordinateType,
                                                             pCoordinateDirection, "-180.0",
                                                             "180.0");
                            default:
                                return string.Empty;
                        }
                    return CoordinateTypeUtils.GetCoodinateUnknownFormatMessage(pCoordinateType, pCoordinateDirection);
                case CoordinateType.Degreeminutesecond:
                    try
                    {
                        value = GetCoordinateDegreesFromDegreeMinuteSecond(pValue);
                        switch (pCoordinateDirection)
                        {
                            case CoordinateDirection.NorthSouth:
                                if (value >= -90.0 && value <= 90.0) return string.Empty;
                                return CoordinateTypeUtils.GetCoodinateOutOfRangeMessage(pCoordinateType,
                                                             pCoordinateDirection,
                                                             DecimalDegreestoDegreeMinuteSecond(
                                                               -90.0, pCoordinateDirection),
                                                             DecimalDegreestoDegreeMinuteSecond(
                                                               90.0, pCoordinateDirection));
                            case CoordinateDirection.WestEast:
                                if (value >= -180.0 && value <= 180.0) return string.Empty;
                                return CoordinateTypeUtils.GetCoodinateOutOfRangeMessage(pCoordinateType,
                                                             pCoordinateDirection,
                                                             DecimalDegreestoDegreeMinuteSecond(
                                                               -180.0, pCoordinateDirection),
                                                             DecimalDegreestoDegreeMinuteSecond(
                                                               180.0, pCoordinateDirection));
                            default:
                                return string.Empty;
                        }
                    }
                    catch (ArgumentException)
                    {
                        return CoordinateTypeUtils.GetCoodinateUnknownFormatMessage(pCoordinateType, pCoordinateDirection);
                    }
                // Rijksdriehoek.
                case CoordinateType.Rd:
                    if (double.TryParse(pValue, out value))
                        switch (pCoordinateDirection)
                        {
                            case CoordinateDirection.NorthSouth:
                                if (value >= 289000.0 && value <= 629000.0) return string.Empty;
                                return CoordinateTypeUtils.GetCoodinateOutOfRangeMessage(pCoordinateType,
                                                             pCoordinateDirection, "289000.0",
                                                             "629000.0");

                            case CoordinateDirection.WestEast:
                                if (value >= -7000.0 && value <= 300000.0) return string.Empty;
                                return CoordinateTypeUtils.GetCoodinateOutOfRangeMessage(pCoordinateType,
                                                             pCoordinateDirection, "-7000.0",
                                                             "300000.0");
                            default:
                                return string.Empty;
                        }
                    return CoordinateTypeUtils.GetCoodinateUnknownFormatMessage(pCoordinateType, pCoordinateDirection);
                default:
                    return string.Empty;
            }
        }

        #endregion

        /// <summary>
        /// Converts the coordinate from raw (map) format to the selected coordinate unit.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="coordinateType">Type of the coordinate.</param>
        /// <returns>
        /// The point in the selected coordinate system
        /// </returns>
        public static SharpMap.Geometries.Point ConvertCoordinateFromXY(SharpMap.Geometries.Point point, CoordinateType coordinateType)
        {
            double latitude, longitude;
            switch (coordinateType)
            {
                case CoordinateType.Degrees:
                    XYPointToLatLonDegrees(point.X, point.Y, out latitude, out longitude);
                    return new SharpMap.Geometries.Point(latitude, longitude);
                case CoordinateType.Rd:
                    XYPointToLatLonDegrees(point.X, point.Y, out latitude, out longitude);
                    double x, y;
                    LonLat2Rd(longitude, latitude, out x, out y);
                    return new SharpMap.Geometries.Point(x, y);
                default:
                    return point;
            }
        }

        /// <summary>
        /// Converts the coordinate from raw (map) format to the selected coordinate unit.
        /// </summary>
        /// <param name="box">The box.</param>
        /// <param name="coordinateType">Type of the coordinate.</param>
        /// <returns>
        /// The bounding box in the selected coordinate system
        /// </returns>
        public static BoundingBox ConvertCoordinateFromXY(BoundingBox box, CoordinateType coordinateType)
        {
            switch (coordinateType)
            {
                case CoordinateType.Rd:
                case CoordinateType.Degrees:
                    var bl = ConvertCoordinateFromXY(box.BottomLeft, coordinateType);
                    var tr = ConvertCoordinateFromXY(box.TopRight, coordinateType);
                    return new BoundingBox(bl, tr);
                default:
                    return box;
            }
        }

        /// <summary>
        /// Converts the coordinate from the selected coordinate unit to raw (map) format.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="coordinateType">Type of the coordinate.</param>
        /// <returns>
        /// The point in the selected coordinate system
        /// </returns>
        public static SharpMap.Geometries.Point ConvertCoordinateToXY(SharpMap.Geometries.Point point, CoordinateType coordinateType)
        {
            switch (coordinateType)
            {
                case CoordinateType.Rd:
                    double latitude, longitude;
                    Rd2LonLat(point.X, point.Y, out latitude, out longitude);
                    var p = LatLonDegreesToXYPoint(latitude, longitude);
                    return new SharpMap.Geometries.Point(p.X, p.Y);
                case CoordinateType.Degrees:
                    p = LatLonDegreesToXYPoint(point.Y, point.X);
                    return new SharpMap.Geometries.Point(p.X, p.Y);
                default:
                    return point;
            }
        }

        /// <summary>
        /// Converts the coordinate from the selected coordinate unit to raw (map) format.
        /// </summary>
        /// <param name="box">The box.</param>
        /// <param name="coordinateType">Type of the coordinate.</param>
        /// <returns>
        /// The bounding box in the selected coordinate system
        /// </returns>
        public static BoundingBox ConvertCoordinateToXY(BoundingBox box, CoordinateType coordinateType)
        {
            switch (coordinateType)
            {
                case CoordinateType.Rd:
                case CoordinateType.Degrees:
                    var bl = ConvertCoordinateToXY(box.BottomLeft, coordinateType);
                    var tr = ConvertCoordinateToXY(box.TopRight, coordinateType);
                    return new BoundingBox(bl, tr);
                default:
                    return box;
            }
        }



        //public static KmlPoint GetKmlPoint(double lon, double lat) {
        //  var lonRadians = (DegreesToRadians*lon);
        //  var latRadians = (DegreesToRadians*lat);

        //  var x = Radius*lonRadians;
        //  var y = Radius*Math.Log(Math.Tan(Math.PI*0.25 + latRadians*0.5));

        //  return new KmlPoint((float) x, (float) y);
        //}

        //public static KmlPoint ToLonLat(double x, double y) {
        //  var ts = Math.Exp(-y/(Radius));
        //  var latRadians = HalfPi - 2*Math.Atan(ts);

        //  var lonRadians = x/(Radius);

        //  var lon = (lonRadians/DegreesToRadians);
        //  var lat = (latRadians/DegreesToRadians);

        //  return new KmlPoint((float) lon, (float) lat);
        //}

        //public static double Distance(KmlPoint point1, KmlPoint point2) { return Distance(point1.LATITUDE, point1.LONGITUDE, point2.LATITUDE, point2.LONGITUDE, 'K'); }
    }
}