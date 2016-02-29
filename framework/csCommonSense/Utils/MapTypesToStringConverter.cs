using System;
using System.ComponentModel;
using System.Globalization;

namespace csShared.Utils
{
  public class MapTypesToStringConverter : TypeConverter
  {
    public override object ConvertTo
      (ITypeDescriptorContext pContext, CultureInfo pCulture, object pValue, Type pDestinationType) {
      //var mapTypes = (MapTypes) pValue;

      //switch (mapTypes) {
      //  case MapTypes.GoogleMaps:
      //    return "Google Maps";        
      //  case MapTypes.GoogleSat:
      //    return "Google Sat";
      //  case MapTypes.GoogleTerrain:
      //    return "Google Terrain";
      //  case MapTypes.OSM:
      //    return "OSM";
      //  case MapTypes.Bing:
      //    return "Bing Aerial";
      //  case MapTypes.BingHybrid:
      //    return "Bing Hybrid";
      //  case MapTypes.BingRoads:
      //    return "Bing Roads";
      //  case MapTypes.Midnight:
      //    return "Midnight Commander";
      //  case MapTypes.Candy:
      //    return "Candy";
      //  case MapTypes.Clean:
      //    return "Clean";
      //  case MapTypes.RedAlert:
      //    return "Red Alert";
      //  case MapTypes.Tourist:
      //    return "Tourist";
      //  case MapTypes.WaterVsLand:
      //    return "Water vs Land";
      //  default:
      //    return "Unknown map type";
      //}
      return "";
      }
  }
}