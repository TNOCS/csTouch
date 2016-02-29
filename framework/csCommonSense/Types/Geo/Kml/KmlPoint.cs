using System;
using System.Globalization;
using System.Xml.Linq;
using System.Xml.Serialization;
using csShared.Utils;

namespace csShared.Geo
{
  [Serializable]
  public class KmlPoint : AGeometry
  {
    private float _mAltitude;
    private float _mLatitude;
    private float _mLongitude;

    public KmlPoint()
    {
    }

    /// <summary>
    /// A geographic location defined by longitude, latitude, and (optional) altitude. When a Point is contained by a Placemark, the point itself determines the position of the Placemark's name and icon. When a Point is extruded, it is connected to the ground with a line. This "tether" uses the current LineStyle.
    /// </summary>
    /// <param name="longitude">between -180 and 180</param>
    /// <param name="latitude">between -90 and 90</param>
    public KmlPoint(float longitude, float latitude)
    {
      Longitude = longitude;
      Latitude = latitude;
    }

    public KmlPoint(double longitude, double latitude)
    {
      Longitude = (float) longitude;
      Latitude = (float) latitude;
    }

    public KmlPoint(double longitude, double latitude, double altitude)
    {
      Longitude = (float) longitude;
      Latitude = (float) latitude;
      Altitude = (float) altitude;
    }

    /// <summary>
    /// A geographic location defined by longitude, latitude, and (optional) altitude. When a Point is contained by a Placemark, the point itself determines the position of the Placemark's name and icon. When a Point is extruded, it is connected to the ground with a line. This "tether" uses the current LineStyle.
    /// </summary>
    /// <param name="longitude">between -180 and 180</param>
    /// <param name="latitude">between -90 and 90</param>
    /// <param name="altitude">altitude values (optional) are in meters above sea level</param>
    public KmlPoint(float longitude, float latitude, float altitude)
    {
      Longitude = longitude;
      Latitude = latitude;
      Altitude = altitude;
    }

    public KmlPoint(XElement e)
    {
      if (e != null)
      {
        XElement el = e.GetElement("coordinates");
        Coordinates = el.Value;
      }
    }

    /// <summary>
    /// longitude between -180 and 180
    /// </summary>
    public float Longitude
    {
      get { return _mLongitude; }
      set
      {
        //if (value < -180 || value > 180) {
        //  throw new NotSupportedException("Longitude must be between -180 and 180");
        //}
        _mLongitude = value;
      }
    }

    /// <summary>
    /// latitude between -90 and 90
    /// </summary>
    public float Latitude
    {
      get { return _mLatitude; }
      set
      {
        //if (value < -90 || value > 90) {
        //  throw new NotSupportedException("Latitude must be between -90 and 90");
        //}
        _mLatitude = value;
      }
    }

    /// <summary>
    /// altitude values (optional) are in meters above sea level
    /// </summary>
    public float Altitude
    {
      get { return _mAltitude; }
      set { _mAltitude = value; }
    }

    [XmlIgnore]
    public string Coordinates
    {
      get
      {
        return _mAltitude == 0
              ? _mLongitude.ToString(CultureInfo.InvariantCulture) + "," +
               _mLatitude.ToString(CultureInfo.InvariantCulture)
              : _mLongitude + "," + _mLatitude + "," + _mAltitude;
      }
      set
      {
        string[] bits = StringUtils.Split(value, ",");
        if (bits.Length < 2) Fail(value);
        if (!float.TryParse(bits[0], NumberStyles.Float, CultureInfo.InvariantCulture, out _mLongitude))
          Fail(value);
        if (!float.TryParse(bits[1], NumberStyles.Float, CultureInfo.InvariantCulture, out _mLatitude))
          Fail(value);
        if (bits.Length == 3)
        {
          if (!float.TryParse(bits[2], NumberStyles.Float, CultureInfo.InvariantCulture, out _mAltitude))
            Fail(value);
        }
      }
    }

    public override string ToString()
    {
      return Latitude.ToString(CultureInfo.InvariantCulture) + "," +
          Longitude.ToString(CultureInfo.InvariantCulture);
    }

    private void Fail(string value)
    {
      throw new Exception("coordinates string not valid: " + value);
    }

    public XElement KmlExport()
    {
      var result = new XElement("Point");
      string c = Longitude.ToString(CultureInfo.InvariantCulture) + "," +
            Latitude.ToString(CultureInfo.InvariantCulture) + Altitude.ToString(CultureInfo.InvariantCulture);
      result.Add(new XElement("coordinates", c));
      return result;
    }
  }
}