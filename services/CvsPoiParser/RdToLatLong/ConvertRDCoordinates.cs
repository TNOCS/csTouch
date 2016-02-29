using System;
using System.Diagnostics;
using System.Linq;

namespace RdToLatLong
{
    // TODO It seems this class is not used. It was present in two projects, so I made a new project to avoid duplicate code. 

    /// <summary>
    ///     Conversion between Dutch Rijksdriehoeksmeting and GPS coordinates.
    ///     Berend Engelbrecht, Decos Software Engineering BV, February 2012.
    /// </summary>
    public class ConvertRdCoordinates
    {
        // Rijksdriehoeksmeting to WGS84 transformation.
        // Formulas from:
        // Benaderingsformules voor de transformatie tussen RD- en WGS84-kaartcoördinaten
        // ing. F.H. Schreutelkamp en ir. G.L. Strang van Hees
        // http://www.dekoepel.nl/pdf/Transformatieformules.pdf

        // Offset and scale to units of 100 km (55)
        private const int X0 = 155000;
        private const int Y0 = 463000;
        private const double XyScale = 100000.0; // 10^5

        // Center point of map (Amersfoort)
        private const double Lat0 = 52.15517440;
        private const double Long0 = 5.38720621;

        /// <summary>
        ///     Calculates d^p for integer values of p >= 0
        /// </summary>
        /// <param name="d">base value</param>
        /// <param name="p">power</param>
        /// <returns>d^p</returns>
        private static double pow(double d, int p)
        {
            double dRet = 1;
            for (int i = 0; i < p; i++)
                dRet *= d;
            return dRet;
        }

        /// <summary>
        ///     Converts Rijksdriehoeksmeting coordinates in meters to GPS coordinates in Lat/Long.
        /// </summary>
        /// <param name="iX">RD x coordinate</param>
        /// <param name="iY">RD y coordinate</param>
        /// <param name="dblLongitude">Longitude</param>
        /// <param name="dblLatitude">Latitude</param>
        public static void RDToLatLong(int iX, int iY, out double dblLatitude, out double dblLongitude)
        {
            dblLatitude = 0.0;
            dblLongitude = 0.0;
            try
            {
                // Transformation parameters
                int[] kp = {0, 2, 0, 2, 0, 2, 1, 4, 2, 4, 1};
                int[] kq = {1, 0, 2, 1, 3, 2, 0, 0, 3, 1, 1};
                double[] kpq =
                {
                    3235.65389, -32.58297, -0.2475, -0.84978, -0.0655, -0.01709, -0.00738, 0.0053, -0.00039,
                    0.00033, -0.00012
                };

                int[] lp = {1, 1, 1, 3, 1, 3, 0, 3, 1, 0, 2, 5};
                int[] lq = {0, 1, 2, 0, 3, 1, 1, 2, 4, 2, 0, 0};
                double[] lpq =
                {
                    5260.52916, 105.94684, 2.45656, -0.81885, 0.05594, -0.05607, 0.01199, -0.00256, 0.00128,
                    0.00022, -0.00022, 0.00026
                };

                double x = (iX - X0)/XyScale;
                double y = (iY - Y0)/XyScale;

                double dLat = kp.Select((t, i) => kpq[i]*pow(x, t)*pow(y, kq[i])).Sum();
                double dLong = lp.Select((t, i) => lpq[i]*pow(x, t)*pow(y, lq[i])).Sum();

                dblLatitude = Lat0 + dLat/3600;
                dblLongitude = Long0 + dLong/3600;
            }
            catch (Exception ex)
            {
                Trace.Write(ex);
            }
        }

        /// <summary>
        ///     Converts latitude, longitude to RD x,y coordinates in meters.
        /// </summary>
        /// <param name="dblLongitude">Longitude</param>
        /// <param name="dblLatitude">Latitude</param>
        /// <param name="iX">RD x coordinate</param>
        /// <param name="iY">RD y coordinate</param>
        public static void LatLongToRD(double dblLatitude, double dblLongitude, out int iX, out int iY)
        {
            iX = 0;
            iY = 0;

            try
            {
                // Transformation parameters
                int[] rp = {0, 1, 2, 0, 1, 3, 1, 0, 2};
                int[] rq = {1, 1, 1, 3, 0, 1, 3, 2, 3};
                double[] rpq = {190094.945, -11832.228, -114.221, -32.391, -0.705, -2.34, -0.608, -0.008, 0.148};

                int[] sp = {1, 0, 2, 1, 3, 0, 2, 1, 0, 1};
                int[] sq = {0, 2, 0, 2, 0, 1, 2, 1, 4, 4};
                double[] spq = {309056.544, 3638.893, 73.077, -157.984, 59.788, 0.433, -6.439, -0.032, 0.092, -0.054};

                double dLat = 0.36*(dblLatitude - Lat0);
                double dLong = 0.36*(dblLongitude - Long0);

                double dx = rp.Select((t, i) => rpq[i]*pow(dLat, t)*pow(dLong, rq[i])).Sum();
                double dy = sp.Select((t, i) => spq[i]*pow(dLat, t)*pow(dLong, sq[i])).Sum();

                iX = X0 + (int) Math.Round(dx);
                iY = Y0 + (int) Math.Round(dy);
            }
            catch (Exception ex)
            {
                Trace.Write(ex);
            }
        }
    }
}