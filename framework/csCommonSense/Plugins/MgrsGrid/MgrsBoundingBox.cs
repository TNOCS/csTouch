using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csCommon.Plugins.MgrsGrid
{
    public class MgrsBoundingBox
    {

        public MgrsBoundingBox(System.Windows.Point[] pPoints)
        {
            var result = new WGS84LatLongPoint[4];
            bool init = false;
            foreach (var pnt in pPoints)
            {
                if (!init)
                {
                    init = true;
                    result[0] = WGS84LatLongPoint.Create(pnt.Y, pnt.X);
                    result[1] = WGS84LatLongPoint.Create(pnt.Y, pnt.X); ;
                    result[2] = WGS84LatLongPoint.Create(pnt.Y, pnt.X); ;
                    result[3] = WGS84LatLongPoint.Create(pnt.Y, pnt.X); ;
                }
                else
                {
                    result[0].Longitude = Math.Min(result[0].Longitude, pnt.X);
                    result[0].Latitude = Math.Max(result[0].Latitude, pnt.Y);

                    result[1].Longitude = Math.Max(result[1].Longitude, pnt.X);
                    result[1].Latitude = Math.Max(result[1].Latitude, pnt.Y);

                    result[2].Longitude = Math.Max(result[2].Longitude, pnt.X);
                    result[2].Latitude = Math.Min(result[2].Latitude, pnt.Y);

                    result[3].Longitude = Math.Min(result[3].Longitude, pnt.X);
                    result[3].Latitude = Math.Min(result[3].Latitude, pnt.Y);
                }
            }
            UpperLeft = WGS84LatLongPoint.Create(result[0].Latitude, result[0].Longitude);
            UpperRight = WGS84LatLongPoint.Create(result[1].Latitude, result[1].Longitude);
            LowerRight = WGS84LatLongPoint.Create(result[2].Latitude, result[2].Longitude);
            LowerLeft = WGS84LatLongPoint.Create(result[3].Latitude, result[3].Longitude);
        }
        

        public WGS84LatLongPoint UpperLeft { get; private set; }
        public WGS84LatLongPoint UpperRight { get; private set; }
        public WGS84LatLongPoint LowerRight { get; private set; }
        public WGS84LatLongPoint LowerLeft { get; private set; }

        public WGS84LatLongPoint CenterOfMass()
        {

            double centroidX = 0.0;
            double centroidY = 0.0;
            centroidX += UpperLeft.Longitude;
            centroidY += UpperLeft.Latitude;
            centroidX += UpperRight.Longitude;
            centroidY += UpperRight.Latitude;
            centroidX += LowerRight.Longitude;
            centroidY += LowerRight.Latitude;
            centroidX += LowerLeft.Longitude;
            centroidY += LowerLeft.Latitude;
            centroidX /= 4;
            centroidY /= 4;

            return WGS84LatLongPoint.Create(centroidY, centroidX);

        }
    }
}
