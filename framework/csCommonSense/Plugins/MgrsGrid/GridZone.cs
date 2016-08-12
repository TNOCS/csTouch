using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csCommon.Plugins.MgrsGrid
{
    using System.Diagnostics;

    using DocumentFormat.OpenXml.Spreadsheet;

    using GeoUtility.GeoSystem;
    using GeoUtility.GeoSystem.Helper;

    using Humanizer;

    // The intersection of a UTM zone and a latitude band is (normally) a 6° × 8° polygon called a grid zone

    [DebuggerDisplay("GZD {GridZoneDesignator}")]
    public class GridZone
    {
        // these class variables are geographic lat/lng
        private double nlat = 0;

        private double slat = 0;

        private double wlng = 0;
        private double elng = 0;

        private double centerlat = 0;

        private double centerlng = 0;

        public GridZone(double pSouthLatitude, double pNorthLatitude, double pWestLongitude, double pEastLongitude)
        {

            nlat = pNorthLatitude;
            slat = pSouthLatitude;

            // special case: Norway
            if (AlmostEqual(pSouthLatitude, 56) && AlmostEqual(pWestLongitude, 0))
            {
                wlng = pWestLongitude;
                elng = pEastLongitude - 3;
            }
            else if (AlmostEqual(pSouthLatitude, 56) && AlmostEqual(pWestLongitude, 6))
            {
                wlng = pWestLongitude - 3;
                elng = pEastLongitude;
            }
            // special case: Svalbard
            else if (AlmostEqual(pSouthLatitude, 72) && AlmostEqual(pWestLongitude, 0))
            {
                wlng = pWestLongitude;
                elng = pEastLongitude + 3;
            }
            else if (AlmostEqual(pSouthLatitude, 72) && AlmostEqual(pWestLongitude, 12))
            {
                wlng = pWestLongitude - 3;
                elng = pEastLongitude + 3;
            }
            else if (AlmostEqual(pSouthLatitude, 72) && AlmostEqual(pWestLongitude, 36))
            {
                wlng = pWestLongitude - 3;
                elng = pEastLongitude;
            }
            else
            {
                wlng = pWestLongitude;
                elng = pEastLongitude;
            }
            this.centerlat = (this.nlat + this.slat) / 2;
            this.centerlng = (this.wlng + this.elng) / 2;
            GridZoneDesignator = SouthWest().ConvertToUtm().Zoneband;
            Extend = GeographicBoundingBox.Create(this.NorthWest(), this.SouthEast());
        }

        public string GridZoneDesignator { get; private set; }

        public static bool AlmostEqual(double x, double y)
        {
            double epsilon = Math.Max(Math.Abs(x), Math.Abs(y)) * 1E-15;
            return Math.Abs(x - y) <= epsilon;
        }

        public GeographicBoundingBox Extend { get; private set; }


        public WGS84LatLongPoint getCenter()
        {
            return WGS84LatLongPoint.Create(this.centerlat, centerlng);
        }

        public WGS84LatLongPoint NorthWest()
        {
            return WGS84LatLongPoint.Create(this.nlat, wlng);

        }
        public WGS84LatLongPoint SouthWest()
        {
            return WGS84LatLongPoint.Create(this.slat, wlng);

        }
        public WGS84LatLongPoint SouthEast()
        {
            return WGS84LatLongPoint.Create(this.slat, elng);

        }
        public WGS84LatLongPoint NorthEast()
        {
            return WGS84LatLongPoint.Create(this.nlat, elng);

        }





    }
}
