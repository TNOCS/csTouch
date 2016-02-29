using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace csCommon.Types.DataServer.Interfaces
{
    public interface IConvertibleGeoJson
    {
        /// <summary>
        /// Convert the object to a GeoJSON representation.
        /// </summary>
        /// <returns>The GeoJSON string.</returns>
        string ToGeoJson();

        /// <summary>
        /// Read the GeoJSON string and set the appropriate attributes of this object (or a new object).
        /// </summary>
        /// <param name="geoJson">Input.</param>
        /// <param name="newObject">Whether to change the object at hand, or to return a new one.</param>
        /// <returns>The object initialized from the string.</returns>
        IConvertibleGeoJson FromGeoJson(string geoJson, bool newObject = true);

        /// <summary>
        /// Read the GeoJSON JObject and set the appropriate attributes of this object (or a new object).
        /// </summary>
        /// <param name="geoJsonObject">Input.</param>
        /// <param name="newObject">Whether to change the object at hand, or to return a new one.</param>
        /// <returns>The object initialized from the string.</returns>
        IConvertibleGeoJson FromGeoJson(JObject geoJsonObject, bool newObject = true);
    }
}
