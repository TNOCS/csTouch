using Caliburn.Micro;

namespace csShared.Geo
{
  public class Camera : PropertyChangedBase
  {

    private double _orientation;

    public double Orientation
    {
      get { return _orientation; }
      set { _orientation = value; }
    }
    

    private KmlPoint _location;

    public KmlPoint Location
    {
      get { return _location; }
      set { _location = value; }
    }

    private double _resolution;

    public double Resolution
    {
      get { return _resolution; }
      set { _resolution = value; }
    }

    private double _tilt;

    public double Tilt
    {
      get { return _tilt; }
      set { _tilt = value; }
    }

    private bool _flyOver;

    public bool FlyOver
    {
      get { return _flyOver; }
      set { _flyOver = value; }
    }
    

  }

  
}
