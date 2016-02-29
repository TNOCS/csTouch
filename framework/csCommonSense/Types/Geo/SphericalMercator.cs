using System;
using System.Windows;
using ESRI.ArcGIS.Client.Geometry;

namespace csShared.Geo
{
  public static class SphericalMercator
  {
    private const double Radius = 6378137;
    private const double D2R = Math.PI/180;
    private const double HalfPi = Math.PI/2;

    public static Point FromLonLat(double lon, double lat)
    {
      double lonRadians = (D2R*lon);
      double latRadians = (D2R*lat);

      double x = Radius*lonRadians;
      double y = Radius*Math.Log(Math.Tan(Math.PI*0.25 + latRadians*0.5));

      return new Point((float) x, (float) y);
    }

    public static Point FromKmlPoint(KmlPoint p)
    {
      return FromLonLat(p.Longitude, p.Latitude);
    }


    public static KmlPoint GetKmlPoint(double lon, double lat)
    {
      double lonRadians = (D2R*lon);
      double latRadians = (D2R*lat);

      double x = Radius*lonRadians;
      double y = Radius*Math.Log(Math.Tan(Math.PI*0.25 + latRadians*0.5));

      return new KmlPoint((float) x, (float) y);
    }


    public static KmlPoint ToLonLat(double x, double y)
    {
      double ts;
      ts = Math.Exp(-y/(Radius));
      double latRadians = HalfPi - 2*Math.Atan(ts);

      double lonRadians = x/(Radius);

      double lon = (lonRadians/D2R);
      double lat = (latRadians/D2R);

      return new KmlPoint((float) lon, (float) lat);
    }

    public static double Distance(KmlPoint point1, KmlPoint point2)
    {
      return Distance(point1.Latitude, point1.Longitude, point2.Latitude, point2.Longitude, 'K');
    }

    public static double Distance(Point point1, Point point2)
    {
      return Math.Sqrt(Math.Abs(point1.X - point2.X) + Math.Abs(point1.Y - point2.Y));
    }

    public static double Distance(MapPoint mp1, MapPoint mp2)
    {
      Polygon pl = new Polygon();
      pl.Rings.Add(new PointCollection() { mp1, mp2});
      return Geodesic.Length(pl);      
    }

    public static double Distance(double lat1, double lon1, double lat2, double lon2, char unit)
    {
      double theta = lon1 - lon2;
      double dist = Math.Sin(Deg2Rad(lat1))*Math.Sin(Deg2Rad(lat2)) +
             Math.Cos(Deg2Rad(lat1))*Math.Cos(Deg2Rad(lat2))*Math.Cos(Deg2Rad(theta));
      dist = Math.Acos(dist);
      dist = Rad2Deg(dist);
      dist = dist*60*1.1515;
      if (unit == 'K')
      {
        dist = dist*1.609344;
      }
      else if (unit == 'N')
      {
        dist = dist*0.8684;
      }
      return (dist);
    }

    //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
    //:: This function converts decimal degrees to radians       :::
    //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
    private static double Deg2Rad(double deg)
    {
      return (deg*Math.PI/180.0);
    }

    //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
    //:: This function converts radians to decimal degrees       :::
    //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
    private static double Rad2Deg(double rad)
    {
      return (rad/Math.PI*180.0);
    }
  }
}