using System;
using System.Globalization;
using System.Xml.Linq;
using csShared.Utils;

namespace csShared.Geo
{
  [Serializable]
  public abstract class ALatLonBox
  {
    private double _mEast;
    private double _mNorth;
    private double _mSouth;
    private double _mWest;

    public ALatLonBox()
    {
    }

    public ALatLonBox(XElement e)
    {
      Parse(e);
    }


    public ALatLonBox(double n, double s, double e, double w)
    {
      _mNorth = n;
      _mSouth = s;
      _mEast = e;
      _mWest = w;
    }

    public ALatLonBox(ALatLonBox box)
    {
      _mNorth = box.North;
      _mSouth = box.South;
      _mEast = box.East;
      _mWest = box.West;
    }

    public double North
    {
      get { return _mNorth; }
      set { _mNorth = value; }
    }

    public double South
    {
      get { return _mSouth; }
      set { _mSouth = value; }
    }

    public double East
    {
      get { return _mEast; }
      set { _mEast = value; }
    }

    public double West
    {
      get { return _mWest; }
      set { _mWest = value; }
    }

    public void Parse(XElement e)
    {
      XElement no = e.GetElement("north");
      if (no != null) North = Convert.ToDouble(no.Value, CultureInfo.InvariantCulture);

      XElement so = e.GetElement("south");
      if (so != null) South = Convert.ToDouble(so.Value, CultureInfo.InvariantCulture);

      XElement ea = e.GetElement("east");
      if (ea != null) East = Convert.ToDouble(ea.Value, CultureInfo.InvariantCulture);

      XElement we = e.GetElement("west");
      if (we != null) West = Convert.ToDouble(we.Value, CultureInfo.InvariantCulture);
    }
  }

  [Serializable]
  public class LatLonBox : ALatLonBox
  {
    private double _mRotation;
    public bool RotationSpecified;

    public LatLonBox()
    {
    }

    public LatLonBox(XElement e)
    {
      Parse(e);

      XElement ro = e.Element("rotation");
      if (ro != null) Rotation = Convert.ToDouble(ro.Value, CultureInfo.InvariantCulture);
    }

    public LatLonBox(double n, double s, double e, double w)
      : base(n, s, e, w)
    {
    }

    public LatLonBox(ALatLonBox box)
      : base(box)
    {
    }

    public double Rotation
    {
      get { return _mRotation; }
      set
      {
        _mRotation = value;
        RotationSpecified = true;
      }
    }

    public string ToBBoxString()
    {
      return West.ToString(CultureInfo.InvariantCulture) + "," +
          South.ToString(CultureInfo.InvariantCulture) + "," +
          East.ToString(CultureInfo.InvariantCulture) + "," +
          North.ToString(CultureInfo.InvariantCulture);
    }
  }

  /*

  <LatLonAltBox>
   <north>43.374</north>
   <south>42.983</south>
   <east>-0.335</east>
   <west>-1.423</west>
   <minAltitude>0</minAltitude>
   <maxAltitude>0</maxAltitude>
  </LatLonAltBox>
 
   <LatLonBox>
  <north>...</north>           <! kml:angle90 -->
  <south>...</south>           <! kml:angle90 -->
  <east>...</east>            <! kml:angle180 -->
  <west>...</west>            <! kml:angle180 -->
  <rotation>0</rotation>         <! kml:angle180 -->
 </LatLonBox>
   */
}