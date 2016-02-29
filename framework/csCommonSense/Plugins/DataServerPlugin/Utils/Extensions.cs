using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataServer;
using ESRI.ArcGIS.Client.Geometry;

namespace csDataServerPlugin.Utils
{
    public static class Extensions
    {
        public static MapPoint ToMapPoint(this Position point)
        {
            if (point == null) return new MapPoint(0, 0);
            return new MapPoint
            {
                X = point.Longitude,
                Y = point.Latitude
            };
        }
    }
}
