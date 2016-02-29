using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace csCommon.MapPlugins.FindLocation
{
    // Program.cs
    public static class EMClass
    {
        public static List<XElement> GetLocalElements(this XElement s, string localname)
        {
            if (s == null) return new List<XElement>();
            return s.Elements().Where(k => k.Name.LocalName == localname).ToList();
        }

        public static XElement GetFirstElement(this XElement s, string localname)
        {
            List<XElement> r = s.Elements().Where(k => k.Name.LocalName == localname).ToList();
            if (r != null && r.Count > 0) return r.First();
            return null;
        }

        public static T GetElementValue<T>(this XElement s, string elementName, T defaultValue) where T : IConvertible
        {
            XElement e = s.GetFirstElement(elementName);
            if (e != null)
            {
                try
                {

                    return GenericConverter.Parse<T>(e.Value);
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;

        }

        public static T GetValue<T>(this XElement s, T defaultValue) where T : IConvertible
        {
            if (s != null)
            {
                try
                {

                    return GenericConverter.Parse<T>(s.Value);
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;

        }


    }

    public class GenericConverter
    {
        public static T Parse<T>(string sourceValue) where T : IConvertible
        {
            return (T)Convert.ChangeType(sourceValue, typeof(T), CultureInfo.InvariantCulture);
        }

        public static T Parse<T>(string sourceValue, IFormatProvider provider) where T : IConvertible
        {
            return (T)Convert.ChangeType(sourceValue, typeof(T), provider);
        }
    }
}
