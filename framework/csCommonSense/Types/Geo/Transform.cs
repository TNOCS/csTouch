using System;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;

namespace csShared.Geo
{
  public class Transform : PropertyChangedBase
  {
    private readonly MatrixTransform _transform = new MatrixTransform();
    private Point _center;
    private Rect _extent;
    private double _height;

    private double _resolution;
    //number of worldunits (meters or degrees) per screen unit (usually pixel, could be inch)

    private double _width;


    public Transform()
    {
      _transform.Matrix = new Matrix();
    }

    public LatLonBox ExtentWorld
    {
      get
      {
        KmlPoint westsouth = SphericalMercator.ToLonLat(_extent.Left, _extent.Bottom);
        KmlPoint eastnorth = SphericalMercator.ToLonLat(_extent.Right, _extent.Top);
        return new LatLonBox(westsouth.Latitude, eastnorth.Latitude, eastnorth.Longitude, westsouth.Longitude);
      }
    }

    public double Resolution
    {
      get { return _resolution; }
      set
      {
        _resolution = value;
        _extent = UpdateExtent(_resolution, _center, _width, _height);
        NotifyOfPropertyChange(() => Resolution);
      }
    }

    public Point Center
    {
      set
      {
        _center = value;
        _extent = UpdateExtent(_resolution, _center, _width, _height);
      }
      get
      {
        KmlPoint p = SphericalMercator.ToLonLat((_extent.Left + _extent.Right)/2,
                            (_extent.Bottom + _extent.Top)/2);
        return new Point(p.Latitude, p.Longitude);
      }
    }

    public double Width
    {
      set
      {
        _width = value;
        _extent = UpdateExtent(_resolution, _center, _width, _height);
      }
      get { return _width; }
    }

    public double Height
    {
      set
      {
        _height = value;
        _extent = UpdateExtent(_resolution, _center, _width, _height);
      }
      get { return _height; }
    }

    public Rect Extent
    {
      get { return _extent; }
    }

    public void Pan(Point currentMap, Point previousMap)
    {
      Point current = MapToWorld(currentMap.X, currentMap.Y);
      Point previous = MapToWorld(previousMap.X, previousMap.Y);
      Vector diff = Point.Subtract(previous, current);
      Center = Point.Add(_center, diff);
    }

    public void Pan(Vector translate)
    {
      var vector = new Vector(-translate.X*_resolution, translate.Y*_resolution);
      _center = Point.Add(_center, vector);
      UpdateExtent(_resolution, _center, _width, _height);
    }

    private Rect UpdateExtent(double resolution, Point center, double width, double height)
    {
      try
      {
        if ((width == 0) || (height == 0)) return new Rect();

        double spanX = width*resolution;
        double spanY = height*resolution;
        var rect = new Rect(center.X - spanX*0.5, center.Y - spanY*0.5, spanX, spanY);

        var matrix = new Matrix();
        double mapCenterX = width*0.5;
        double mapCenterY = height*0.5;

        matrix.Translate(mapCenterX - center.X, mapCenterY - center.Y);

        matrix.ScaleAt(1/resolution, 1/resolution, mapCenterX, mapCenterY);

        matrix.Append(new Matrix(1, 0, 0, -1, 0, 0));
        matrix.Translate(0, height);

        _transform.Matrix = matrix;

        return rect;
      }
      catch
      {
        return new Rect();
      }
    }

    public void Zoom(Rect zoomRect, Rect prevZoomRect)
    {
      Matrix matrix = _transform.Matrix;
      matrix.Translate(-GetCenterX(prevZoomRect), -GetCenterY(prevZoomRect));
      double scale = zoomRect.Width/prevZoomRect.Width;
      matrix.Scale(scale, scale);
      matrix.Translate(GetCenterX(zoomRect), GetCenterY(zoomRect));
      _transform.Matrix = matrix;
    }

    public void ScaleAt(double scale, Point origin)
    {
      Matrix matrix = _transform.Matrix;
      matrix.ScaleAt(scale, scale, origin.X, origin.Y);

      _transform.Matrix = matrix;
      if (_transform.Inverse == null) return; //happens when extermely zoomed out.
      _center = _transform.Inverse.Transform(new Point(_width/2, _height/2));
      _resolution = _resolution/scale;
      _extent = UpdateExtent(_resolution, _center, _width, _height);
    }

    public void RotateAt(double angle, Point origin)
    {
      Matrix matrix = _transform.Matrix;
      matrix.RotateAt(angle, origin.X, origin.Y);

      _transform.Matrix = matrix;
      if (_transform.Inverse == null) return; //happens when extermely zoomed out.
      _center = _transform.Inverse.Transform(new Point(_width/2, _height/2));
      //resolution = resolution / scale;
      _extent = UpdateExtent(_resolution, _center, _width, _height);
    }

    private static Point FindBoundaries(double twidth, double theight, Point p1)
    {
      if (p1.X < 0) p1.X = 0;
      if (p1.Y < 0) p1.Y = 0;
      if (p1.X > twidth) p1.X = twidth;
      if (p1.Y > theight) p1.Y = theight;
      return p1;
    }

    public bool Contains(KmlPoint p)
    {
      return Extent.Contains(SphericalMercator.FromKmlPoint(p));
    }

    public bool Contains(Rect r)
    {
      //if (this.extent.Contains(r)) return true;
      //if (r.X < this.extent.Right && (r.Y > this.extent.Y || 

      double twidth = Application.Current.MainWindow.Width;
      double theight = Application.Current.MainWindow.Height;
      Point p1 = WorldToMap(r.X, r.Y);
      Point p2 = WorldToMap(r.Right, r.Bottom);

      p1 = FindBoundaries(twidth, theight, p1);
      p2 = FindBoundaries(twidth, theight, p2);


      double square = Math.Abs(p2.X - p1.X)*Math.Abs(p2.Y - p1.Y);
      double res = square/(twidth*theight);

      return (res > 0);
    }

    public Point WorldToMap(double x, double y)
    {
      Point point;
      point = _transform.Transform(new Point(x, y));
      return point;
    }

    public Point MapPoint(KmlPoint p)
    {
      Point world = SphericalMercator.FromLonLat(p.Longitude, p.Latitude);
      return WorldToMap(world.X, world.Y);
    }

    public Point MapToWorld(double x, double y)
    {
      Point point = new Point();
      GeneralTransform inverseTransform = _transform.Inverse;

      if (inverseTransform != null) point = inverseTransform.Transform(new Point(x, y));
      return point;
    }

    public Vector MapToWorld(Vector vector)
    {
      Point point = new Point();
      GeneralTransform inverseTransform = _transform.Inverse;
      if (inverseTransform != null)
        point = inverseTransform.Transform(new Point(vector.X - _center.X, vector.Y - _center.Y));
      return new Vector(point.X, point.Y);
    }


    private static double GetCenterX(Rect rect)
    {
      return ((rect.Left + rect.Right)*0.5F);
    }

    private static double GetCenterY(Rect rect)
    {
      return ((rect.Top + rect.Bottom)*0.5F);
    }
  }
}