using System;

namespace csShared.Geo
{
  [Serializable]
  public abstract class AGeometry
  {
    public bool AltitudeModeSpecified;
    private bool _mExtrude;
    private bool _mTessellate;
    private AltitudeMode _mAltitudeMode = AltitudeMode.RelativeToGround;

    protected AGeometry()
    {
    }

    public bool Tessellate
    {
      get { return _mTessellate; }
      set { _mTessellate = value; }
    }

    public bool Extrude
    {
      get { return _mExtrude; }
      set { _mExtrude = value; }
    }

    public AltitudeMode AltitudeMode
    {
      get { return _mAltitudeMode; }
      set
      {
        _mAltitudeMode = value;
        AltitudeModeSpecified = true;
      }
    }
  }
}