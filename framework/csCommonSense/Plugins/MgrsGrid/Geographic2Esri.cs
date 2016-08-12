using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csCommon.Plugins.MgrsGrid
{
    using System.Diagnostics;

    using ESRI.ArcGIS.Client.Geometry;
    using ESRI.ArcGIS.Client.Projection;



    public class NotSupportedSpatialReferenceException : Exception
    {
        public NotSupportedSpatialReferenceException(string pMessage)
            : base(pMessage)
        {
        }
    }

    public static class GeometryConverter
    {
        public static SpatialReference Wgs84SpatialRef = new SpatialReference(4326);
        public static SpatialReference WebMercatorSpatialRef = new SpatialReference(102100);
        public static WebMercator Mercator = new WebMercator();

        // ESRI throw exception when latitude/longitude are out of range
        public static double ToEsriCorrectLongitude(double pLongitude)
        {
            return Math.Min(Math.Max(-180, pLongitude), 180);
        }

        public static double ToEsriCorrectLatitude(double pLatitude)
        {
            return Math.Min(Math.Max(-80, pLatitude), 80);
        }

        // http://www.gal-systems.com/2011/07/convert-coordinates-between-web.html
        private static void ToGeographic(ref double mercatorX_lon, ref double mercatorY_lat)
        {
            if (Math.Abs(mercatorX_lon) < 180 && Math.Abs(mercatorY_lat) < 90)
                return;

            if ((Math.Abs(mercatorX_lon) > 20037508.3427892) || (Math.Abs(mercatorY_lat) > 20037508.3427892))
                return;

            double x = mercatorX_lon;
            double y = mercatorY_lat;
            double num3 = x / 6378137.0;
            double num4 = num3 * 57.295779513082323;
            double num5 = Math.Floor((double)((num4 + 180.0) / 360.0));
            double num6 = num4 - (num5 * 360.0);
            double num7 = 1.5707963267948966 - (2.0 * Math.Atan(Math.Exp((-1.0 * y) / 6378137.0)));
            mercatorX_lon = num6;
            mercatorY_lat = num7 * 57.295779513082323;
        }

        public static MapPoint ToWebMercator(WGS84LatLongPoint pPoint)
        {
            //Gaat fout bij lat=90 en lat-90?!  return Mercator.FromGeographic(new MapPoint(pPoint.Longitude.Value, pPoint.Latitude.Value, Wgs84SpatialRef)) as MapPoint;
            double mercatorY_lat = pPoint.Latitude;
            double mercatorX_lon = pPoint.Longitude;
            while (mercatorY_lat > 90) mercatorY_lat -= 90;
            while (mercatorY_lat < -90) mercatorY_lat += 90;
            while (mercatorX_lon < -180) mercatorX_lon += 180;
            while (mercatorX_lon > 180) mercatorX_lon -= 180;
            if (mercatorY_lat == 90) mercatorY_lat = 89.9; // Bug fix: prevent infinity
            if (mercatorY_lat == -90) mercatorY_lat = -89.9; // Bug fix: prevent infinity
            if ((Math.Abs(mercatorX_lon) > 180 || Math.Abs(mercatorY_lat) > 90))
                return new MapPoint(0, 0, WebMercatorSpatialRef);

            double num = mercatorX_lon * 0.017453292519943295;
            double x = 6378137.0 * num;
            double a = mercatorY_lat * 0.017453292519943295;

            mercatorX_lon = x;
            mercatorY_lat = 3189068.5 * Math.Log((1.0 + Math.Sin(a)) / (1.0 - Math.Sin(a)));
            Debug.Assert(!Double.IsNegativeInfinity(mercatorY_lat), "infinity when converting coordinates");
            Debug.Assert(!Double.IsPositiveInfinity(mercatorY_lat), "infinity when converting coordinates");
            return new MapPoint(mercatorX_lon, mercatorY_lat, WebMercatorSpatialRef);
        }






        private static SpatialReference GetSpatialReference(int pExpectedWkid)
        {
            switch (pExpectedWkid)
            {
                case 102100: return WebMercatorSpatialRef;
                case 4326: return Wgs84SpatialRef;
                default: throw new NotImplementedException();
            }
        }



        public static MapPoint ToEsriGeometry(this WGS84LatLongPoint pPoint, int pExpectedWkid)
        {
            switch (pExpectedWkid)
            {
                case 4326: return new MapPoint(ToEsriCorrectLongitude(pPoint.Longitude), ToEsriCorrectLatitude(pPoint.Latitude), Wgs84SpatialRef);

                case 102100:
                    return ToWebMercator(pPoint);

                default:
                    throw new NotImplementedException();
            }

        }


        public static Envelope ToEsriGeometry(this GeographicBoundingBox pBoundingBox, int pExpectedWkid)
        {
            switch (pExpectedWkid)
            {
                case 4326:
                    var result = new Envelope(
    new MapPoint(pBoundingBox.WestBoundLongitude, pBoundingBox.NorthBoundLatitude, GetSpatialReference(pExpectedWkid)),
    new MapPoint(pBoundingBox.EastBoundLongitude, pBoundingBox.SouthBoundLatitude, GetSpatialReference(pExpectedWkid))
    );
                    result.SpatialReference = GetSpatialReference(pExpectedWkid);
                    return result;

                case 102100:
                    var result1 = new Envelope(
                        ToWebMercator(
                            WGS84LatLongPoint.Create(pBoundingBox.NorthBoundLatitude, pBoundingBox.WestBoundLongitude
                                                         )),
                            ToWebMercator(WGS84LatLongPoint.Create(pBoundingBox.SouthBoundLatitude, pBoundingBox.EastBoundLongitude
                                                                       )));
                    result1.SpatialReference = WebMercatorSpatialRef;
                    return result1;
                default:
                    throw new NotImplementedException();
            }
        }

        public static Geometry CorrectSpatialRef(this Geometry pGeometry, int pExpectedWkid)
        {
            if ((pGeometry.SpatialReference != null) && (pGeometry.SpatialReference.WKID != pExpectedWkid))
            {
                if ((pGeometry.SpatialReference.WKID == 102100) && (pExpectedWkid == 4326))
                    return Mercator.ToGeographic(pGeometry);
                if ((pGeometry.SpatialReference.WKID == 4326) && (pExpectedWkid == 102100))
                    return Mercator.FromGeographic(pGeometry);
            }
            return pGeometry;
        }


        public static WGS84LatLongPoint ToGenericGeometry(this MapPoint pPoint)
        {
            return ToGenericGeometry(pPoint, -1);
        }

        public static WGS84LatLongPoint ToGenericGeometry(this MapPoint pPoint, int pExpectedWKID)
        {
            if ((pPoint.SpatialReference != null) || (pExpectedWKID != -1))
            {
                int wkid = (pPoint.SpatialReference != null) ? pPoint.SpatialReference.WKID : pExpectedWKID;
                switch (wkid)
                {
                    case 102100:
                        return (Mercator.ToGeographic(pPoint) as MapPoint).ToGenericGeometry(4326);
                    case 4326:
                        return WGS84LatLongPoint.Create(pPoint.Y, pPoint.X);
                    default:
                        throw new NotSupportedSpatialReferenceException(
                            String.Format("SpatialReference {0} not supported for MapPoint",
                            pPoint.SpatialReference.WKID));
                }
            }
            throw new NotSupportedSpatialReferenceException("SpatialReference not supported (MapPoint)");
        }




        public static GeographicBoundingBox ToGenericGeometry(this Envelope pEnvelope)
        {
            return ToGenericGeometry(pEnvelope, -1);
        }

        public static GeographicBoundingBox ToGenericGeometry(this Envelope pEnvelope, int pExpectedWKID)
        {
            if ((pEnvelope.SpatialReference != null) || (pExpectedWKID != -1))
            {
                int wkid = (pEnvelope.SpatialReference != null) ? pEnvelope.SpatialReference.WKID : pExpectedWKID;
                switch (wkid)
                {
                    case 102100:
                        return (Mercator.ToGeographic(pEnvelope) as Envelope).ToGenericGeometry(4326);
                    case 4326:
                        return GeographicBoundingBox.Create(
                            WGS84LatLongPoint.Create(pEnvelope.YMax, pEnvelope.XMin),
                            WGS84LatLongPoint.Create(pEnvelope.YMin, pEnvelope.XMax));
                    default:
                        throw new NotSupportedSpatialReferenceException(
                            String.Format("SpatialReference {0} not supported for Envelope",
                            pEnvelope.SpatialReference.WKID));
                }
            }
            throw new NotSupportedSpatialReferenceException("SpatialReference not supported (Envelope)");
        }



    }


    /*


        */
    public class GeoMath
    {
        /// <summary>
        /// The distance type to return the results in.
        /// </summary>
        public enum MeasureUnits
        {
            Miles, Kilometers
        };


        /// <summary>
        /// Returns the distance in miles or kilometers of any two
        /// latitude / longitude points. (Haversine formula)
        /// </summary>
        public static double Distance(double latitudeA, double longitudeA, double latitudeB, double longitudeB, MeasureUnits units)
        {
            if (latitudeA <= -90 || latitudeA >= 90 || longitudeA <= -180 || longitudeA >= 180
                || latitudeB <= -90 && latitudeB >= 90 || longitudeB <= -180 || longitudeB >= 180)
            {
                throw new ArgumentException(String.Format("Invalid value point coordinates. Points A({0},{1}) B({2},{3}) ",
                                                          latitudeA,
                                                          longitudeA,
                                                          latitudeB,
                                                          longitudeB));
            }


            double R = (units == MeasureUnits.Miles) ? 3960 : 6371;
            double dLat = toRadian(latitudeB - latitudeA);
            double dLon = toRadian(longitudeB - longitudeA);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(toRadian(latitudeA)) * Math.Cos(toRadian(latitudeB)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
            double d = R * c;
            return d;
        }



        /// <summary>
        /// Convert to Radians.
        /// </summary>      
        private static double toRadian(double val)
        {
            return (Math.PI / 180) * val;
        }

        private static double toDegrees(double val)
        {
            return (180 / Math.PI) * val;
        }
        public const double EarthRadius = 6378137.0;

        /// <summary>
        /// Calculates the end-point from a given source at a given range (meters) and bearing (degrees).
        /// This methods uses simple geometry equations to calculate the end-point.
        /// </summary>
        /// <param name="source">Point of origin</param>
        /// <param name="range">Range in meters</param>
        /// <param name="bearing">Bearing in degrees</param>
        /// <returns>End-point from the source given the desired range and bearing.</returns>
        public static WGS84LatLongPoint CalculateDerivedPosition(WGS84LatLongPoint source, double range, double bearing)
        {
            double latA = toRadian(source.Latitude);
            double lonA = toRadian(source.Longitude);
            double angularDistance = range / EarthRadius;
            double trueCourse = toRadian(bearing);

            double lat = Math.Asin(
                Math.Sin(latA) * Math.Cos(angularDistance) +
                Math.Cos(latA) * Math.Sin(angularDistance) * Math.Cos(trueCourse));

            double dlon = Math.Atan2(
                Math.Sin(trueCourse) * Math.Sin(angularDistance) * Math.Cos(latA),
                Math.Cos(angularDistance) - Math.Sin(latA) * Math.Sin(lat));

            double lon = ((lonA + dlon + Math.PI) % (Math.PI * 2)) - Math.PI;

            return WGS84LatLongPoint.Create(toDegrees(lat), toDegrees(lon));
        }


    }
}
