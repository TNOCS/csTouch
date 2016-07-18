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

    public class WGS84LatLongPoint
    {
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


    }
}
