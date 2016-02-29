using System;
using System.Globalization;
using System.Windows.Media;
using System.Xml.Linq;

namespace DataServer
{
    public static class XElementExt
    {
        public static string GetString(this XElement element, string name) {
            return element.Attribute(name) != null ? element.Attribute(name).Value : null;
        }

        public static string GetString(this XElement element, string name, string defaultValue) {
            return element.Attribute(name) != null ? element.Attribute(name).Value : defaultValue;
        }

        public static Guid GetGuid(this XElement element, string name) {
            return element.Attribute(name) != null ? Guid.Parse(element.Attribute(name).Value) : Guid.Empty;
        }

        public static bool GetBool(this XElement element, string name, bool defaultValue = false) {
            return element.Attribute(name) != null ? bool.Parse(element.Attribute(name).Value) : defaultValue;
        }


        public static bool? GetNullBool(this XElement element, string name)
        {
            return element.Attribute(name) != null ? bool.Parse(element.Attribute(name).Value) : new bool?();
        }

        public static DateTime GetDate(this XElement element, string name, DateTime defaultValue) {
            return element.Attribute(name) != null ? DateTime.Parse(element.Attribute(name).Value) : defaultValue;
        }

        public static Color GetColor(this XElement element, string name) {
            return element.Attribute(name) != null
                ? ColorReflector.ToColorFromHex(element.Attribute(name).Value)
                : Colors.Transparent;
            //return (Color) ColorConverter.ConvertFromString(element.Attribute(name).Value);
        }

        

        public static Color GetColor(this XElement element, string name, string defaultColor) {
            return
                ColorReflector.ToColorFromHex(element.Attribute(name) != null
                    ? element.Attribute(name).Value
                    : element.Attribute(defaultColor).Value);
            //return (Color) ColorConverter.ConvertFromString(element.Attribute(name).Value);
        }

        public static Color GetColor(this XElement element, string name, Color defaultColor) {
            return element.Attribute(name) != null
                ? ColorReflector.ToColorFromHex(element.Attribute(name).Value)
                : defaultColor;
            //return (Color) ColorConverter.ConvertFromString(element.Attribute(name).Value);
        }

        public static Color? GetNullColor(this XElement element, string name)
        {
            return element.Attribute(name) != null
                ? ColorReflector.ToColorFromHex(element.Attribute(name).Value)
                : new Color?();
            //return (Color) ColorConverter.ConvertFromString(element.Attribute(name).Value);
        }

        public static Double GetDouble(this XElement element, string name) {
            return element.Attribute(name) != null
                ? Double.Parse(element.Attribute(name).Value, CultureInfo.InvariantCulture)
                : 0.0;
        }

        public static Double GetDouble(this XElement element, string name, double defaultValue) {
            return element.Attribute(name) != null
                ? Double.Parse(element.Attribute(name).Value, CultureInfo.InvariantCulture)
                : defaultValue;
        }

        public static Double? GetNullDouble(this XElement element, string name)
        {
            return element.Attribute(name) != null
                ? Double.Parse(element.Attribute(name).Value, CultureInfo.InvariantCulture)
                : new double?();
        }

        public static int GetInt(this XElement element, string name, int defaultValue = 0) {
            return element.Attribute(name) != null ? Int32.Parse(element.Attribute(name).Value) : defaultValue;
        }

        public static int? GetNullInt(this XElement element, string name)
        {
            return element.Attribute(name) != null ? Int32.Parse(element.Attribute(name).Value) : new int?();
        }

        public static long GetLong(this XElement element, string name, long defaultValue = 0) {
            try {
                return element.Attribute(name) != null ? Int64.Parse(element.Attribute(name).Value) : defaultValue;
            }
            catch (Exception) {
                return defaultValue;
            }
        }

        public static Uri GetUri(this XElement element, string name) {
            return element.Attribute(name) != null ? new Uri(element.Attribute(name).Value) : new Uri("file://");
        }
    }
}