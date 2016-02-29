namespace csShared.Geo
{
  public class LatLonAltBox : ALatLonBox
  {
    private double _mMaxAltitude;
    private double _mMinAltitude;
    public bool MaxAltitudeSpecified;
    public bool MinAltitudeSpecified;

    public LatLonAltBox()
    {
    }

    public LatLonAltBox(double n, double s, double e, double w)
      : base(n, s, e, w)
    {
    }

    public LatLonAltBox(ALatLonBox box)
      : base(box)
    {
    }

    public double MinAltitude
    {
      get { return _mMinAltitude; }
      set
      {
        _mMinAltitude = value;
        MinAltitudeSpecified = true;
      }
    }

    public double MaxAltitude
    {
      get { return _mMaxAltitude; }
      set
      {
        _mMaxAltitude = value;
        MaxAltitudeSpecified = true;
      }
    }

    public bool Contains(KmlPoint point)
    {
      return true;
    }
  }
}