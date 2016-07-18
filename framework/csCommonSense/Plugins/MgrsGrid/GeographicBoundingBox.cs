using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csCommon.Plugins.MgrsGrid
{
    public class GeographicBoundingBox 
    {

        public static GeographicBoundingBox Create(WGS84LatLongPoint pUpperLeft, WGS84LatLongPoint pLowerRight)
        {
            // switch east and west, because the bounding box is positioned around the 180 degree meridian
            return new GeographicBoundingBox()
            {
                NorthBoundLatitude = Math.Max(pUpperLeft.Latitude, pLowerRight.Latitude),
                WestBoundLongitude = Math.Min(pUpperLeft.Longitude, pLowerRight.Longitude),
                SouthBoundLatitude = Math.Min(pUpperLeft.Latitude, pLowerRight.Latitude),
                EastBoundLongitude = Math.Max(pUpperLeft.Longitude, pLowerRight.Longitude)
            };

        }

        public double WestBoundLongitude { get; set; }
        public double EastBoundLongitude { get; set; }
        public double SouthBoundLatitude { get; set; }
        public double NorthBoundLatitude { get; set; }

    }
}
