using csShared.Properties;
using System;
using System.ComponentModel;
using System.Globalization;

namespace csShared.Utils
{
    /// <summary>
    ///  Specifies the coordinate type that you want to use for displaying the mouse coordinate
    /// </summary>
    [TypeConverter(typeof(CoordinateTypeToStringConverter))]
    public enum CoordinateType
    {
        Degreeminutesecond,
        Degrees,
        Rd,
        Xy
    }

    /// <summary>
    ///  Specifies the unit type to use
    /// </summary>
    //[TypeConverter(typeof(UnitToStringConverter))]
    public enum Unit
    {
        Metric,
        Imperial
    }

    public class CoordinateTypeToStringConverter : TypeConverter
    {
        public override object ConvertTo
          (ITypeDescriptorContext pContext, CultureInfo pCulture, object pValue, Type pDestinationType)
        {
            var coordinateType = (CoordinateType)pValue;

            switch (coordinateType)
            {
                case CoordinateType.Degreeminutesecond:
                    return CoordinateTypes.COORDINATE_FORMAT_DEGREEMINUTESECOND;
                case CoordinateType.Degrees:
                    return CoordinateTypes.COORDINATE_FORMAT_DEGREES;
                case CoordinateType.Rd:
                    return CoordinateTypes.COORDINATE_FORMAT_RD;
                case CoordinateType.Xy:
                    return CoordinateTypes.COORDINATE_FORMAT_XY;
                default:
                    return CoordinateTypes.COORDINATE_FORMAT_UNKNOWN;
            }
        }

        public override object ConvertFrom(ITypeDescriptorContext pContext, CultureInfo pCulture, object pValue)
        {
            var coordinateText = (string)pValue;

            if (CoordinateTypes.COORDINATE_FORMAT_DEGREEMINUTESECOND.Equals(coordinateText)) return CoordinateType.Degreeminutesecond;
            if (CoordinateTypes.COORDINATE_FORMAT_DEGREES.Equals(coordinateText)) return CoordinateType.Degrees;
            if (CoordinateTypes.COORDINATE_FORMAT_RD.Equals(coordinateText)) return CoordinateType.Rd;
            if (CoordinateTypes.COORDINATE_FORMAT_XY.Equals(coordinateText)) return CoordinateType.Xy;

            return CoordinateTypes.COORDINATE_FORMAT_DEGREES;
        }
    }
}