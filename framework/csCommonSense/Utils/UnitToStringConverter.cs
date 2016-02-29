using System;
using System.ComponentModel;
using System.Globalization;

namespace csShared.Utils
{
  public class UnitToStringConverter : TypeConverter
  {
    public override object ConvertTo(ITypeDescriptorContext pContext,
                     CultureInfo pCulture,
                     object pValue,
                     Type pDestinationType)
    {
      var unit = (Unit)pValue;

      switch (unit)
      {
        case Unit.Metric:
          return "Metric";
        case Unit.Imperial:
          return "Imperial";
        default:
          return "Unknown unit";
      }
    }
  }
}