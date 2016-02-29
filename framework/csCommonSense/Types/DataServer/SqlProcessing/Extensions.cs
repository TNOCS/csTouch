using csShared.Geo;
using csShared.Utils;
using SharpMap.Geometries;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DataServer.SqlProcessing
{
    public static partial class Extensions
    {
        /// <summary>
        /// Convert a list of points to closed well known text polygon.
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="coordinateType"></param>
        /// <returns></returns>
        public static string ConvertPointsToWkt(this List<Point> zone, CoordinateType coordinateType)
        {
            var sb = new StringBuilder("'POLYGON((");
            foreach (var p in zone.Select(pt => CoordinateUtils.ConvertCoordinateFromXY(pt, coordinateType)))
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0:} {1},", p.X, p.Y);
            }
            var p0 = CoordinateUtils.ConvertCoordinateFromXY(zone[0], coordinateType);
            sb.AppendFormat(CultureInfo.InvariantCulture, "{0} {1}))'", p0.X, p0.Y); // close the ring
            return sb.ToString();
        }

        /// <summary>
        /// Convert a list of lat/lon points to closed well known text polygon in WGS84.
        /// </summary>
        /// <param name="zone">Points in lat/lon</param>
        /// <returns></returns>
        public static string ConvertPointsToEwkt(this List<System.Windows.Point> zone) {
            var p0 = zone[0];
            var sb = new StringBuilder("'SRID=4326;POLYGON((");
            foreach (var p in zone) {
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0:###.######} {1:###.######},", p.X, p.Y);
            }
            sb.AppendFormat(CultureInfo.InvariantCulture, "{0:###.######} {1:###.######}))'", p0.X, p0.Y); // close the ring
            return sb.ToString();
        }

        /// <summary>
        /// Convert a list of points to closed well known text polygon.
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string ConvertPointsToWkt(this List<System.Windows.Point> zone, CoordinateType type)
        {
            var sb = new StringBuilder("'POLYGON((");
            foreach (var p in zone.Select(pt => CoordinateUtils.ConvertCoordinateFromXY(pt.ToPoint(), type)))
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0} {1},", p.X, p.Y);
            }
            var p0 = CoordinateUtils.ConvertCoordinateFromXY(zone[0].ToPoint(), type);
            sb.AppendFormat(CultureInfo.InvariantCulture, "{0} {1}))'", p0.X, p0.Y); // close the ring
            return sb.ToString();
        }

        public static Point ToPoint(this System.Windows.Point pt) { return new Point(pt.X, pt.Y);}

        public static IEnumerable<System.Windows.Point> ConvertToPointCollection(this string polygon)
        {
            foreach (Match match in regex.Matches(polygon))
            {
                double x, y;
                if (!double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out x) ||
                    !double.TryParse(match.Groups[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out y)) continue;
                var p = new System.Windows.Point(x, y);
                yield return p;
            }
        }


        //  using System.Text.RegularExpressions;

        /// <summary>
        ///  Regular expression built for C# on: Thu, Dec 19, 2013, 11:29:52 PM
        ///  Using Expresso Version: 3.0.4750, http://www.ultrapico.com
        ///  
        ///  A description of the regular expression:
        ///  
        ///  [1]: A numbered capture group. [\d*.\d*]
        ///      \d*.\d*
        ///          Any digit, any number of repetitions
        ///          Any character
        ///          Any digit, any number of repetitions
        ///  Space
        ///  [2]: A numbered capture group. [\d*.\d*]
        ///      \d*.\d*
        ///          Any digit, any number of repetitions
        ///          Any character
        ///          Any digit, any number of repetitions
        ///  
        ///
        /// </summary>
        public static Regex regex = new Regex(
              "(\\d*.\\d*) (\\d*.\\d*)",
            RegexOptions.Multiline
            | RegexOptions.CultureInvariant
            | RegexOptions.Compiled
            );


        //// Replace the matched text in the InputText using the replacement pattern
        // string result = regex.Replace(InputText,regexReplace);

        //// Split the InputText wherever the regex matches
        // string[] results = regex.Split(InputText);

        //// Capture the first Match, if any, in the InputText
        // Match m = regex.Match(InputText);

        //// Capture all Matches in the InputText
        // MatchCollection ms = regex.Matches(InputText);

        //// Test to see if there is a match in the InputText
        // bool IsMatch = regex.IsMatch(InputText);

        //// Get the names of all the named and numbered capture groups
        // string[] GroupNames = regex.GetGroupNames();

        //// Get the numbers of all the named and numbered capture groups
        // int[] GroupNumbers = regex.GetGroupNumbers();



    }
}