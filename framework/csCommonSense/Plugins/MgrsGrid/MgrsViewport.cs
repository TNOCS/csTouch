using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csCommon.Plugins.MgrsGrid
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using ESRI.ArcGIS.Client;

    #endregion

    /*
     USNG is functionally equivalent to MGRS. The difference between the two systems is in the method for specifying the datum. In MGRS, an alternate lettering scheme is used for the 100,000-meter grid square designator when the position is referenced to an older datum (see section on MGRS). The USNG does not use the alternate lettering scheme, but simply specifies the datum after the position reference. For example, a position on the NAD 27 datum is reported in the two systems as follows:

MGRS: “15SWN8083350993”

USNG: “15SWC8083350993 (NAD 27)” 
     * 
     * */

    /// <summary>
    /// Creates 
    /// </summary>
    public class MgrsViewport
    {
        // UTM zones -- geographic lines at 6x8 deg intervals
        #region Constants

        /// <summary>
        /// The correct for zone border.
        /// </summary>
        public const double CorrectForZoneBorder = 0.0000000001; // Otherwise box would be in two zones....

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MgrsViewport"/> class.
        /// </summary>
        /// <param name="pEsriMap">
        /// The p esri map.
        /// </param>
        public MgrsViewport(Map pEsriMap)
        {
            this.EsriMap = pEsriMap;
            this.Extend = pEsriMap.Extent.ToGenericGeometry();
            DiagonalDistanceInKm = GeoMath.Distance(
                Extend.NorthBoundLatitude,
                Extend.WestBoundLongitude,
                Extend.SouthBoundLatitude,
                Extend.EastBoundLongitude,
                GeoMath.MeasureUnits.Kilometers);

            // UTM is undefined beyond 84N or 80S, so this application defines viewport at those limits
            if (this.Extend.NorthBoundLatitude > 84)
            {
                this.Extend.NorthBoundLatitude = 84;
            }

            if (this.Extend.SouthBoundLatitude < -80)
            {
                this.Extend.SouthBoundLatitude = -80;
            }

            this.LatitudeLines = new List<double>(CalculateVisibleLatitudeLines(this.Extend));
            this.LongitudeLines = new List<double>(CalculateVisibleLongitudeLines(this.Extend));
            this.GridZoneUtmCells = CalculateVisibleUtmBoxes(this.LatitudeLines, this.LongitudeLines);
            MapHeight = pEsriMap.ActualHeight;
            MapWidth = pEsriMap.ActualWidth;
        }

        #endregion

        #region Public Properties

        public double DiagonalDistanceInKm { get; private set; }

        public double MetersPerPixel
        {
            get
            {
                return (DiagonalDistanceInKm = GeoMath.Distance(Extend.NorthBoundLatitude, Extend.WestBoundLongitude, Extend.NorthBoundLatitude, Extend.EastBoundLongitude, GeoMath.MeasureUnits.Kilometers) * 1000) / EsriMap.ActualWidth;
            }
        }

        public double MapHeight { get; private set; }
        public double MapWidth { get; private set; }

        /// <summary>
        /// Gets the esri map.
        /// </summary>
        public Map EsriMap { get; private set; }

        /// <summary>
        /// Gets the extend.
        /// </summary>
        public GeographicBoundingBox Extend { get; private set; }

        /// <summary>
        /// Gets the latitude lines.
        /// </summary>
        public List<double> LatitudeLines { get; private set; }

        /// <summary>
        /// Gets the longitude lines.
        /// </summary>
        public List<double> LongitudeLines { get; private set; }

        /// <summary>
        /// All MGRS zones in visible extend. At the edges it can also be partly MGRS zone
        /// </summary>
        public List<GridZone> GridZoneUtmCells { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// The almost equal.
        /// </summary>
        /// <param name="x">
        /// The x.
        /// </param>
        /// <param name="y">
        /// The y.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private static bool AlmostEqual(double x, double y)
        {
            double epsilon = Math.Max(Math.Abs(x), Math.Abs(y)) * 1E-15;
            return Math.Abs(x - y) <= epsilon;
        }

        /// <summary>
        /// Return list of latitudes in bounding box
        ///     UTM Latitude: each 8 degrees
        /// </summary>
        /// <param name="pBoundingBox">
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable"/>.
        /// </returns>
        private static IEnumerable<double> CalculateVisibleLatitudeLines(GeographicBoundingBox pBoundingBox)
        {
            var result = new List<double>();
            result.Add(pBoundingBox.SouthBoundLatitude);
            double y1 = (pBoundingBox.SouthBoundLatitude < -80)
                            ? -80
                            : (Math.Floor((pBoundingBox.SouthBoundLatitude / 8) + 1) * 8.0);

            for (var currentLatitude = y1; currentLatitude < pBoundingBox.NorthBoundLatitude; currentLatitude += 8)
            {
                if (currentLatitude <= 72)
                {
                    result.Add(currentLatitude);
                }
                else if (currentLatitude <= 80)
                {
                    result.Add(84);
                }
            }

            result.Add(pBoundingBox.NorthBoundLatitude);
            return result.Distinct();
        }

        /// <summary>
        /// Return list of latitudes in bounding box
        ///     UTM Longitude: each 6 degrees
        /// </summary>
        /// <param name="pBoundingBox">
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable"/>.
        /// </returns>
        private static IEnumerable<double> CalculateVisibleLongitudeLines(GeographicBoundingBox pBoundingBox)
        {
            var result = new List<double>();

            // first zone intersection inside the southwest corner of the map window
            // longitude coordinate is straight-forward...
            double x1 = Math.Floor((pBoundingBox.WestBoundLongitude / 6.0) + 1) * 6.0;
            result.Add(pBoundingBox.WestBoundLongitude);
            if (pBoundingBox.WestBoundLongitude < pBoundingBox.EastBoundLongitude)
            {
                // normal case
                for (double currentLongitude = x1, j = 1;
                     currentLongitude < pBoundingBox.EastBoundLongitude;
                     currentLongitude += 6, j++)
                {
                    result.Add(currentLongitude);
                }
            }
            else
            {
                Debug.Assert(false, "Boundingbox problem with map");
            }

            result.Add(pBoundingBox.EastBoundLongitude);
            return result.Distinct();
        }

        /// <summary>
        /// Create UTM boxes (can be full utm cell of partly utm cell (at edges map))
        /// </summary>
        /// <param name="pLatitudeLines">
        /// </param>
        /// <param name="pLongitudeLines">
        /// </param>
        /// <returns>
        /// The <see cref="List"/>.
        /// </returns>
        private static List<GridZone> CalculateVisibleUtmBoxes(
            List<double> pLatitudeLines,
            List<double> pLongitudeLines)
        {
            var result = new List<GridZone>();
            for (int i = 0; i < pLatitudeLines.Count - 1; i++)
            {
                for (int j = 0; j < pLongitudeLines.Count - 1; j++)
                {
                    if ((pLatitudeLines[i] >= 72 && AlmostEqual(pLongitudeLines[j], 6))
                        || (pLatitudeLines[i] >= 72 && AlmostEqual(pLongitudeLines[j], 18))
                        || (pLatitudeLines[i] >= 72 && AlmostEqual(pLongitudeLines[j], 30)))
                    {
                        // do nothing
                    }
                    else
                    {
                        // Substract small
                        var dta = new GridZone(
                            pLatitudeLines[i],
                            pLatitudeLines[i + 1] - CorrectForZoneBorder,
                            pLongitudeLines[j],
                            pLongitudeLines[j + 1] - CorrectForZoneBorder);
                        result.Add(dta);

                        // Console.WriteLine(String.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", dta.nlat, dta.wlng, dta.slat, dta.elng));
                    }
                }
            }

            return result;
        }

        #endregion
    }
}
