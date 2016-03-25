using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NETGeographicLib;

namespace csCommon.Converters
{
    /// <summary>
    /// Class for conversion between Latitude-Longitude coordinates and MGRS (Military Grid Reference System) coordinates.
    /// For more details, see the documentation of NETGeographicLib [http://geographiclib.sourceforge.net/html/NET/].
    /// </summary>
    public class MgrsConversion
    {
        /// <summary>
        /// Returns a string representation the MGRS coordinates, with the highest level of precision (1 m).
        /// </summary>
        /// <param name="lat"> Latitude in degrees </param>
        /// <param name="lon"> Longitude in degrees </param>
        public static string convertLatLonToMgrs(double lat, double lon)
        {
            int zone;
            bool northp;
            double x, y;
            UTMUPS.Forward(lat, lon, out zone, out northp, out x, out y, -1, true);
            string mgrs;
            MGRS.Forward(zone, northp, x, y, lat, 5, out mgrs);
            return mgrs;
        }

        /// <summary>
        /// Returns a string representation the MGRS coordinates, with specified precision.
        /// Precision should be specified with the prec parameter:
        /// prec = 1 -> 10 km precision
        /// prec = 2 -> 1 km precision
        /// prec = 3 -> 100 m precision
        /// prec = 4 -> 10 m precision
        /// prec = 5 -> 1 m precision
        /// </summary>
        /// <param name="lat"> Latitude in degrees </param>
        /// <param name="lon"> Longitude in degrees </param>
        /// <param name="prec"> Precision parameter (see description) </param>

        public static string convertLatLonToMgrsWithPrecision(double lat, double lon, int prec)
        {
            int zone;
            bool northp;
            double x, y;
            UTMUPS.Forward(lat, lon, out zone, out northp, out x, out y, -1, true);
            string mgrs;
            MGRS.Forward(zone, northp, x, y, lat, prec, out mgrs);
            return mgrs;
        }

        /// <summary>
        /// Returns a double array with [lat, lon] in degrees
        /// </summary>
        /// <param name="mgrs"> MGRS string </param>
        public static double[] convertMgrsToLatLon(string mgrs)
        {
            int zone, prec;
            bool northp;
            double x, y;
            MGRS.Reverse(mgrs, out zone, out northp, out x, out y, out prec, true);
            double lat, lon;
            UTMUPS.Reverse(zone, northp, x, y, out lat, out lon, true);
            double[] latLon = new double[] {lat, lon};
            return latLon;
        }
    }
}
