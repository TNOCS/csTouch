using System;
using System.IO;
using System.Linq;
using System.Text;
using csCommon.Types.DataServer.Interfaces;
using csCommon.Utils.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace csCommon.Types.DataServer.PoI.IO
{
    // TODO Missing features from GeoJson
    /*
     * • A GeoJSON object may have an optional "crs" member, the value of which must be a coordinate reference system object (see 3. Coordinate Reference System Objects).
     * • A GeoJSON object may have a "bbox" member, the value of which must be a bounding box array (see 4. Bounding Boxes).
     */

    // TODO WellKnownTextIO has huge overlap with Geometries.cs which allows converting shapes to WKT and back.
    // TODO WellKnownTextIO has huge overlap with CoordinateSystemWktReader in one of the libraries.

    public class WellKnownTextIO : IImporter<FileLocation, WellKnownTextIO>, IConvertibleGeoJson // IExporter<WellKnownTextIO, string> removed
    {
        public const string WKT_LABEL = "wkt";

        #region Constructor

        public WellKnownTextIO()
        {
            WktText = "";
        }

        public WellKnownTextIO(string wktText)
        {
            WktText = wktText.ToUpperInvariant();
            if (string.IsNullOrEmpty(ToGeoJson()))
            {
                throw new Exception("Well-known text element is probably not valid.");
            }
        }

        public string WktText { get; private set; }

        public override string ToString()
        {
            return WktText;
        }

        #endregion Constructor

        #region Importer
              
        public string DataFormat
        {
            get { return "Well-known text"; }
        }

        public string DataFormatExtension
        {
            get { return "txt"; }
        }

        public bool EnableValidation { get; set; }

        public IOResult<WellKnownTextIO> ImportData(FileLocation source)
        {
            using (var reader = new StreamReader(source.LocationString, Encoding.ASCII)) // WKT is ASCII
            {
                string wkt = reader.ReadToEnd();
                return new IOResult<WellKnownTextIO>(new WellKnownTextIO(wkt));
            }
        }

        public bool SupportsMetaData
        {
            get { return false; }
        }

        #endregion Importer

        #region IConvertableGeoJson

        public IConvertibleGeoJson FromGeoJson(string geoJson, bool newObject = true)
        {
            JObject jObject = JObject.Parse(geoJson);
            return FromGeoJson(jObject, newObject);
        }

        public IConvertibleGeoJson FromGeoJson(JObject geoJsonObject, bool newObject = true)
        {
            var builder = new StringBuilder();
            switch (geoJsonObject["type"].ToString())
            {
                case "Point":
                    builder.Append(GeoJsonPointToWkt(geoJsonObject["coordinates"].ToString(Formatting.None)));
                    break;
                case "LineString":
                    builder.Append(GeoJsonLineStringToWkt(geoJsonObject["coordinates"].ToString(Formatting.None)));
                    break;
                case "Polygon":
                    builder.Append(GeoJsonPolygonToWkt(geoJsonObject["coordinates"].ToString(Formatting.None)));
                    break;
                case "MultiPoint":
                    builder.Append(GeoJsonMultiPointToWkt(geoJsonObject["coordinates"].ToString(Formatting.None)));
                    break;
                case "MultiLineString":
                    builder.Append(GeoJsonMultiLineStringToWkt(geoJsonObject["coordinates"].ToString(Formatting.None)));
                    break;
                case "MultiPolygon":
                    builder.Append(GeoJsonMultiPolygonToWkt(geoJsonObject["coordinates"].ToString(Formatting.None)));
                    break;
            }

            WellKnownTextIO wkt = newObject ? new WellKnownTextIO() : this;
            wkt.WktText = builder.ToString();
            return wkt;
        }

        public string GeoJsonPointToWkt(string geoJsonPointCoordinates, bool addKeyword = true)
        {
            string keyword = addKeyword ? "POINT (" : "";
            StringBuilder builder = new StringBuilder(keyword); //.Append('(');

            geoJsonPointCoordinates = PreprocessGeoJson(geoJsonPointCoordinates);
            string[] coordinates = geoJsonPointCoordinates.Split(',');
            if (coordinates.Length != 2)
            {
                throw new NotImplementedException("GeoJson to WKT conversion currently only supports coordinate systems with two coordinates.");
            }

            foreach (var coordinate in coordinates)
            {
                builder.Append(coordinate).Append(' ');
            }

            builder.Length--; // Remove the last space.
            if (addKeyword) builder.Append(')');
            return builder.ToString();
        }

        public string GeoJsonLineStringToWkt(string geoJsonLineStringCoordinates, bool addKeyword = true)
        {
            string keyword = addKeyword ? "LINESTRING " : "";
            StringBuilder builder = new StringBuilder(keyword).Append('(');
            GeoJsonLineStringOrMultiPointToWkt(geoJsonLineStringCoordinates, builder);
            return builder.ToString();
        }

        private void GeoJsonLineStringOrMultiPointToWkt(string geoJsonLineStringOrMultiPointCoordinates, StringBuilder builder)
        {
            geoJsonLineStringOrMultiPointCoordinates = PreprocessGeoJson(geoJsonLineStringOrMultiPointCoordinates);
            string[] points = geoJsonLineStringOrMultiPointCoordinates.Split(new[] {"["}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var point in points)
            {
                builder.Append(GeoJsonPointToWkt("[" + point, false)).Append(',');
            }
            builder.Length--; // Remove the last comma.
            builder.Append(')');
        }

        public string GeoJsonPolygonToWkt(string geoJsonPolygonCoordinates, bool addKeyword = true)
        {
            string keyword = addKeyword ? "POLYGON " : "";
            StringBuilder builder = new StringBuilder(keyword).Append('(');
            GeoJsonPolygonOrMultiLineStringToWkt(geoJsonPolygonCoordinates, builder);
            return builder.ToString();
        }

        private void GeoJsonPolygonOrMultiLineStringToWkt(string geoJsonPolygonOrMultiLineStringCoordinates, StringBuilder builder)
        {
            geoJsonPolygonOrMultiLineStringCoordinates = PreprocessGeoJson(geoJsonPolygonOrMultiLineStringCoordinates);
            string[] lineStrings = geoJsonPolygonOrMultiLineStringCoordinates.Split(new[] {"[["}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var lineString in lineStrings)
            {
                builder.Append(GeoJsonLineStringToWkt("[[" + lineString, false)).Append(',');
            }
            builder.Length--; // Remove the last comma.
            builder.Append(')');
        }

        public string GeoJsonMultiPointToWkt(string geoJsonMultiPointCoordinates, bool addKeyword = true)
        {
            string keyword = addKeyword ? "MULTIPOINT " : "";
            StringBuilder builder = new StringBuilder(keyword).Append('(');

            GeoJsonLineStringOrMultiPointToWkt(geoJsonMultiPointCoordinates, builder);
            return builder.ToString();
        }

        public string GeoJsonMultiLineStringToWkt(string geoJsonMultiLineStringCoordinates, bool addKeyword = true)
        {
            string keyword = addKeyword ? "MULTILINESTRING " : "";
            StringBuilder builder = new StringBuilder(keyword).Append('(');
            GeoJsonPolygonOrMultiLineStringToWkt(geoJsonMultiLineStringCoordinates, builder);
            return builder.ToString();
        }

        private string GeoJsonMultiPolygonToWkt(string geoJsonMultiPolygonCoordinates, bool addKeyword = true)
        {
            string keyword = addKeyword ? "MULTIPOLYGON " : "";
            StringBuilder builder = new StringBuilder(keyword).Append('(');
            geoJsonMultiPolygonCoordinates = PreprocessGeoJson(geoJsonMultiPolygonCoordinates);
            string[] polygons = geoJsonMultiPolygonCoordinates.Split(new[] { "[[[" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var polygon in polygons)
            {
                builder.Append(GeoJsonPolygonToWkt("[[[" + polygon, false)).Append(',');
            }
            builder.Length--; // Remove the last comma.
            builder.Append(')');
            return builder.ToString();
        }

        private static string PreprocessGeoJson(string geoJson)
        {
            // Preprocess. Strip any spaces, comma's or square brackets (on the outside).
            geoJson = geoJson.Replace(" ", ""); 
            if (geoJson.StartsWith(","))
            {
                geoJson = geoJson.Substring(1);
            }
            if (geoJson.EndsWith(","))
            {
                geoJson = geoJson.Substring(0, geoJson.Length - 1);
            }
            if (geoJson.StartsWith("["))
            {
                geoJson = geoJson.Substring(1);
            }
            if (geoJson.EndsWith("]"))
            {
                geoJson = geoJson.Substring(0, geoJson.Length - 1);
            }
            return geoJson;
        }

        public string ToGeoJson()
        {
            // Make sure this fails fast if needed!
            if (string.IsNullOrEmpty(WktText))
            {
                return null;
            }

            // Setup a builder.
            var builder = new StringBuilder();

            // Try to parse point.
            if (WktText.StartsWith("POINT"))
            {
                builder.Append(WktPointToGeoJson(WktText));
                return builder.ToString();
            }

            // Try to parse line string.
            if (WktText.StartsWith("LINESTRING"))
            {
                builder.Append(WktLineStringToGeoJson(WktText));
                return builder.ToString();
            }

            // Try to parse polygon.
            if (WktText.StartsWith("POLYGON"))
            {
                builder.Append(WktPolygonToGeoJson(WktText));
                return builder.ToString();
            }

            // Try to parse multi point.
            if (WktText.StartsWith("MULTIPOINT"))
            {
                builder.Append(WktMultiPointToGeoJson(WktText));
                return builder.ToString();
            }

            // Try to parse multi line string.
            if (WktText.StartsWith("MULTILINESTRING"))
            {
                builder.Append(WktMultiLineStringToGeoJson(WktText));
                return builder.ToString();
            }
            
            // Try to parse multi polygon.
            if (WktText.StartsWith("MULTIPOLYGON"))
            {
                builder.Append(WktMultiPolygonToGeoJson(WktText));
                return builder.ToString();
            }

            // Cannot parse anything.
            return null;
        }

        public static string WktMultiPolygonToGeoJson(string wktMultiPolygon)
        {
            return WktToGeoJson(wktMultiPolygon, "MultiPolygon", 4);
        }

        // Input: {MULTILINESTRING {Z|M|EMPTY|}|}  ((a b, c d), (e f))
        public static string WktMultiLineStringToGeoJson(string wktMultiLineString)
        {
            return WktToGeoJson(wktMultiLineString, "MultiLineString", 3);
        }

        // Input: {POLYGON {Z|M|EMPTY|}|}  ((a b, c d), (e f))
        public static string WktPolygonToGeoJson(string wktPolygon)
        {
            return WktToGeoJson(wktPolygon, "Polygon", 3);
        }

        // Input: {MULTIPOINT {Z|M|EMPTY|}|}  ((a b), (c d), (e f))     OR (a b, c d, e f)
        public static string WktMultiPointToGeoJson(string wktMultiPoint)
        {
            return WktToGeoJson(wktMultiPoint, "MultiPoint", 2);
        }

        // Input: (a b, c d)   OR  LINESTRING (e f)    OR      LINESTRING Z... (g h)
        public static string WktLineStringToGeoJson(string wktLineString)
        {
            return WktToGeoJson(wktLineString, "LineString", 2);
        }

        public static string WktPointToGeoJson(string wktPoint)
        {
            return WktToGeoJson(wktPoint, "Point", 1);
        }

        /// <summary>
        /// Common conversion for Wkt to GeoJson.
        /// </summary>
        /// <param name="wktString">What to convert.</param>
        /// <param name="geometryType">The geometry type to convert to (determined in advance).</param>
        /// <param name="depth">The depth, i.e., how many opening brackets before we encounter a coordinate?</param>
        /// <returns></returns>
        /// // TODO  Improvement in code: geometry type and depth can be rather easily derived from the WKT string itself.
        /// // Then, we can switch from having all those methods above to just one method.
        private static string WktToGeoJson(string wktString, string geometryType, int depth)
        {
            // Preprocess. Balance brackets, determine number of coordinates needed in every point, 
            // and determine whether there is a geometry type.
            bool includesGeometryType;
            int numCoordinatesRequired;
            PreprocessWkt(ref wktString, out includesGeometryType, out numCoordinatesRequired);

            // Add the wktString.
            var builder = new StringBuilder();
            builder.Append(wktString);

            // Make the format uniform. Remove all spaces that are not surrounded by numbers.
            builder.Replace(@"[^\d]\s+[^\d]", "");

            // WKT can have points surrounded by ( ) or not; make this uniform.
            string brackets = "";
            for (int i = 0; i < depth; i++)
            {
                brackets = brackets + "(";
            }
            if (!wktString.Contains(brackets)) // Points provided without ( ), so add them.
            {
                builder.Insert(0, "(").Append(")"); 
                builder = builder.Replace(",", "),(");
            }

            // Replace the required characters.
            builder.Replace(" ", ","); // Comma's between the point coordinates.
            builder.Replace("(", "["); // Convert brackets.
            builder.Replace(")", "]"); // Convert brackets.

            // Add the geometry type if needed.
            if (includesGeometryType)
            {
                builder.Insert(0, string.Format("{{\"type\":\"{0}\",\"coordinates\":", geometryType));
                builder.Append("}");
            }

            // Finalize and return.
            return builder.ToString();
        }

        /// <summary>
        ///     Preprocess the WKT element.
        /// </summary>
        /// <param name="wktElement">The WKT element, output with any geometry type removed.</param>
        /// <param name="numCoordinatesRequired">The number of coordinates required; default to -1, which will derive them.</param>
        /// <param name="includesGeometryType">Indicates whether the element contained a geometry type</param>
        private static void PreprocessWkt(ref string wktElement, out bool includesGeometryType,
            out int numCoordinatesRequired)
        {
            numCoordinatesRequired = GetNumCoordinatesRequired(wktElement, out includesGeometryType);
            wktElement = TrimToBrackets(wktElement);
        }

        /// <summary>
        ///     Derive the number of coordinates required, which is always 2, except if Z (3), M (4), or EMPTY (0) are placed
        ///     behind the geometry type.
        /// </summary>
        /// <param name="wktString">The WKT string.</param>
        /// <param name="includesGeometryType">Indicates whether the element contained a geometry type</param>
        /// <returns>The number of coordinates required.</returns>
        private static int GetNumCoordinatesRequired(string wktString, out bool includesGeometryType)
        {
            wktString = wktString.Trim();
            int openingBracketIndex = wktString.IndexOf("(", StringComparison.InvariantCulture);
            if (openingBracketIndex < 0)
            {
                includesGeometryType = false;
                return 2;
            }
            wktString = wktString.Substring(0, openingBracketIndex).Trim();
            if (wktString.Length == 0)
            {
                includesGeometryType = false;
                return 2;
            }
            includesGeometryType = true;
            string[] tokens = wktString.Split(new []{' '}, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 2)
            {
                return 2;
            }
            switch (tokens[1])
            {
                case "EMPTY":
                    return 0;
                case "Z":
                case "z":
                    return 3;
                case "M":
                case "m":
                case "ZM": // According to Wikipedia example...
                case "zm": // According to Wikipedia example...
                    return 4;
            }
            return 2;
        }

        private static string TrimToBrackets(string wktString)
        {
            int startIndex = wktString.IndexOf("(", StringComparison.InvariantCulture); // + 1;
            int endIndex = wktString.LastIndexOf(")", StringComparison.InvariantCulture) + 1;
            if (startIndex == -1)
            {
                wktString = "(" + wktString;
                startIndex = 0; // No opening bracket.
            }
            if (endIndex == -1)
            {
                wktString = wktString + ")";
                endIndex = wktString.Length; // No closing bracket.
            }
            string trimmedWktLineString = wktString.Substring(startIndex, endIndex - startIndex);
            return trimmedWktLineString;
        }

        #endregion IConvertableGeoJson
    }
}