#region

using System;
using System.ComponentModel;
using csShared.Properties;

#endregion

namespace csShared.Utils
{
  public static class CoordinateTypeUtils
  {
    /// <summary>
    ///  Convert a coordinate format string to enum.
    /// </summary>
    /// <param name = "pCoordinateFormatText">The coordinate format text.</param>
    /// <returns></returns>
    public static CoordinateType ConvertToEnum(string pCoordinateFormatText) {
      try {
        return
          (CoordinateType)
          TypeDescriptor.GetConverter(typeof (CoordinateType)).ConvertFromString(pCoordinateFormatText);
      }
      catch (Exception) {
        return CoordinateType.Degrees;
      }
    }

    /// <summary>
    ///  Converts a coordinate type to a coordinate format string.
    /// </summary>
    /// <param name = "pCoordinateType">Type of the coordinate.</param>
    /// <returns></returns>
    public static string ConvertToString(CoordinateType pCoordinateType) { return TypeDescriptor.GetConverter(typeof (CoordinateType)).ConvertToString(pCoordinateType); }

    /// <summary>
    ///  Gets the coordinate label.
    /// </summary>
    /// <returns></returns>
    public static string GetCoordinateLabel
      (CoordinateType pCoordinateType, CoordinateUtils.CoordinateDirection pCoordinateDirection) {
      switch (pCoordinateType) {
          // Latitude-longitude.
        case CoordinateType.Degreeminutesecond:
          switch (pCoordinateDirection) {
            case CoordinateUtils.CoordinateDirection.NorthSouth:
              return CoordinateTypes.COORDINATE_LABEL_DEGREEMINUTESECOND_NS;
            case CoordinateUtils.CoordinateDirection.WestEast:
              return CoordinateTypes.COORDINATE_LABEL_DEGREEMINUTESECOND_WE;
            default:
              return CoordinateTypes.COORDINATE_LABEL_UNKNOWN;
          }
        case CoordinateType.Degrees:
          switch (pCoordinateDirection) {
            case CoordinateUtils.CoordinateDirection.NorthSouth:
              return CoordinateTypes.COORDINATE_LABEL_DEGREES_NS;
            case CoordinateUtils.CoordinateDirection.WestEast:
              return CoordinateTypes.COORDINATE_LABEL_DEGREES_WE;
            default:
              return CoordinateTypes.COORDINATE_LABEL_UNKNOWN;
          }
        case CoordinateType.Rd:
          switch (pCoordinateDirection) {
            case CoordinateUtils.CoordinateDirection.NorthSouth:
              return CoordinateTypes.COORDINATE_LABEL_RD_NS;
            case CoordinateUtils.CoordinateDirection.WestEast:
              return CoordinateTypes.COORDINATE_LABEL_RD_WE;
            default:
              return CoordinateTypes.COORDINATE_LABEL_UNKNOWN;
          }
        case CoordinateType.Xy:
          switch (pCoordinateDirection) {
            case CoordinateUtils.CoordinateDirection.NorthSouth:
              return CoordinateTypes.COORDINATE_LABEL_XY_NS;
            case CoordinateUtils.CoordinateDirection.WestEast:
              return CoordinateTypes.COORDINATE_LABEL_XY_WE;
            default:
              return CoordinateTypes.COORDINATE_LABEL_UNKNOWN;
          }
        default:
          return CoordinateTypes.COORDINATE_LABEL_UNKNOWN;
      }
    }

    /// <summary>
    ///  Gets the coordinate tooltip.
    /// </summary>
    /// <returns></returns>
    public static string GetCoordinateTooltip
      (CoordinateType pCoordinateType, CoordinateUtils.CoordinateDirection pCoordinateDirection) {
      switch (pCoordinateType) {
          // Latitude-longitude.
        case CoordinateType.Degreeminutesecond:
          switch (pCoordinateDirection) {
            case CoordinateUtils.CoordinateDirection.NorthSouth:
              return CoordinateTypes.COORDINATE_LABEL_DEGREEMINUTESECOND_NS;
            case CoordinateUtils.CoordinateDirection.WestEast:
              return CoordinateTypes.COORDINATE_LABEL_DEGREEMINUTESECOND_WE;
            default:
              return CoordinateTypes.COORDINATE_LABEL_UNKNOWN;
          }
        case CoordinateType.Degrees:
          switch (pCoordinateDirection) {
            case CoordinateUtils.CoordinateDirection.NorthSouth:
              return CoordinateTypes.COORDINATE_LABEL_DEGREES_NS;
            case CoordinateUtils.CoordinateDirection.WestEast:
              return CoordinateTypes.COORDINATE_LABEL_DEGREES_WE;
            default:
              return CoordinateTypes.COORDINATE_LABEL_UNKNOWN;
          }
        case CoordinateType.Rd:
          switch (pCoordinateDirection) {
            case CoordinateUtils.CoordinateDirection.NorthSouth:
              return CoordinateTypes.COORDINATE_LABEL_RD_NS;
            case CoordinateUtils.CoordinateDirection.WestEast:
              return CoordinateTypes.COORDINATE_LABEL_RD_WE;
            default:
              return CoordinateTypes.COORDINATE_LABEL_UNKNOWN;
          }
        case CoordinateType.Xy:
          switch (pCoordinateDirection) {
            case CoordinateUtils.CoordinateDirection.NorthSouth:
              return CoordinateTypes.COORDINATE_LABEL_XY_NS;
            case CoordinateUtils.CoordinateDirection.WestEast:
              return CoordinateTypes.COORDINATE_LABEL_XY_WE;
            default:
              return CoordinateTypes.COORDINATE_FORMAT_UNKNOWN;
          }
        default:
          return CoordinateTypes.COORDINATE_LABEL_UNKNOWN;
      }
    }

    /// <summary>
    ///  Gets the coordinate validation error message or empty string if correct.
    /// </summary>
    /// <returns>validation message or empty string</returns>
    //public static string GetCoordinateValidationMessage(CoordinateType pCoordinateType, CoordinateUtils.CoordinateDirection pCoordinateDirection, string pValue) {
    // if (string.IsNullOrEmpty(pValue)) {
    //  return GetCoodinateNotSpecifiedMessage(pCoordinateType, pCoordinateDirection);
    // }
    // return CoordinateUtils.CheckCoordinate(SettingsHelpers.CoordinateType, pCoordinateDirection, pValue);
    //}
    public static string GetCoodinateNotSpecifiedMessage
      (CoordinateType pCoordinateType, CoordinateUtils.CoordinateDirection pCoordinateDirection) {
      return string.Format(CoordinateTypes.MESSAGE_COORDINATE_NOT_SPECIFIED,
                 GetCoodinateString(pCoordinateType, pCoordinateDirection));
    }

    public static string GetCoodinateUnknownFormatMessage
      (CoordinateType pCoordinateType, CoordinateUtils.CoordinateDirection pCoordinateDirection) {
      return string.Format(CoordinateTypes.MESSAGE_COORDINATE_UNKNOWN_FORMAT,
                 GetCoodinateString(pCoordinateType, pCoordinateDirection));
    }

    /// <summary>
    ///  Gets the coodinate out of range message.
    /// </summary>
    /// <param name = "pCoordinateType">Type of the coordinate.</param>
    /// <param name = "pCoordinateDirection">The coordinate direction.</param>
    /// <param name = "pMin">The min value.</param>
    /// <param name = "pMax">The max value.</param>
    /// <returns></returns>
    public static string GetCoodinateOutOfRangeMessage
      (CoordinateType pCoordinateType,
       CoordinateUtils.CoordinateDirection pCoordinateDirection,
       string pMin,
       string pMax) {
      return string.Format(CoordinateTypes.MESSAGE_COORDINATE_OUT_OF_RANGE,
                 GetCoodinateString(pCoordinateType, pCoordinateDirection), pMin, pMax);
    }

    private static string GetCoodinateString
      (CoordinateType pCoordinateType, CoordinateUtils.CoordinateDirection pCoordinateDirection) {
      switch (pCoordinateType) {
          // Latitude-longitude.
        case CoordinateType.Degreeminutesecond:
          switch (pCoordinateDirection) {
            case CoordinateUtils.CoordinateDirection.NorthSouth:
              return CoordinateTypes.COORDINATE_STRING_DEGREEMINUTESECOND_NS;
            case CoordinateUtils.CoordinateDirection.WestEast:
              return CoordinateTypes.COORDINATE_STRING_DEGREEMINUTESECOND_WE;
            default:
              return CoordinateTypes.COORDINATE_STRING_UNKNOWN;
          }
        case CoordinateType.Degrees:
          switch (pCoordinateDirection) {
            case CoordinateUtils.CoordinateDirection.NorthSouth:
              return CoordinateTypes.COORDINATE_STRING_DEGREES_NS;
            case CoordinateUtils.CoordinateDirection.WestEast:
              return CoordinateTypes.COORDINATE_STRING_DEGREES_WE;
            default:
              return CoordinateTypes.COORDINATE_STRING_UNKNOWN;
          }
        case CoordinateType.Rd:
          switch (pCoordinateDirection) {
            case CoordinateUtils.CoordinateDirection.NorthSouth:
              return CoordinateTypes.COORDINATE_STRING_RD_NS;
            case CoordinateUtils.CoordinateDirection.WestEast:
              return CoordinateTypes.COORDINATE_STRING_RD_WE;
            default:
              return CoordinateTypes.COORDINATE_STRING_UNKNOWN;
          }
        case CoordinateType.Xy:
          switch (pCoordinateDirection) {
            case CoordinateUtils.CoordinateDirection.NorthSouth:
              return CoordinateTypes.COORDINATE_STRING_XY_NS;
            case CoordinateUtils.CoordinateDirection.WestEast:
              return CoordinateTypes.COORDINATE_STRING_XY_NS;
            default:
              return CoordinateTypes.COORDINATE_STRING_UNKNOWN;
          }
        default:
          return CoordinateTypes.COORDINATE_LABEL_UNKNOWN;
      }
    }
  }
}