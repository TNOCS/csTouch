using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace DataServer
{

    public static class Utilities
    {

        public static bool ValueOrFalse(this bool? v)
        {
            return (v.HasValue) && v.Value;
        }

        public static string InnerXml(this XElement thiz)
        {
            var xReader = thiz.CreateReader();
            xReader.MoveToContent();
            return xReader.ReadInnerXml();
        }

        public static string InnerXml2(this XElement element)
        {
            var innerXml = new StringBuilder();

            foreach (var node in element.Nodes())
            {
                // append node's xml string to innerXml
                innerXml.Append(node);
            }
            return innerXml.ToString();
        }

        public static XElement RemoveAllNamespaces(this XElement source)
        {
            return source.HasElements
                       ? new XElement(source.Name.LocalName, source.Elements().Select(RemoveAllNamespaces))
                       : new XElement(source.Name.LocalName)
                       {
                           Value = source.Value
                       };
        }

        public static IEnumerable<string> ReadAllLines(this Stream stream, Encoding encoding)
        {
            using (var reader = new StreamReader(stream, encoding))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLineAsync();
                    yield return line.Result;
                }
            }
        }

        public static string Color2Hex(this System.Windows.Media.Color? color)
        {
            if (color == null) return "#fff";
            var c = (System.Windows.Media.Color)color;
            return string.Format("#{0:x2}{1:x2}{2:x2}{3:x2}", c.A, c.R, c.G, c.B);
        }

    }


    public static class ExtensionMethods
    {
        /// <summary>
        /// Long extension method to convert a Unix epoch
        /// time to a standard C# DateTime object.
        /// </summary>
        /// <returns>A DateTime object representing the unix
        /// time as seconds since 1/1/1970</returns>
        public static DateTime FromEpoch(this long unixTime)
        {
            //new DateTime(unixTime, DateTimeKind.Utc);
            return new DateTime(1970, 1, 1).AddSeconds(unixTime);
        }

        /// <summary>
        /// Date Time extension method to return a unix epoch
        /// time as a long
        /// </summary>
        /// <returns> A long representing the Date Time as the number
        /// of seconds since 1/1/1970</returns>
        public static long ToEpoch(this DateTime dt)
        {
            return (long)(dt - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        public static string ToRelativeDate(this DateTime start, DateTime input)
        {
            Dictionary<long, string> thresholds = new Dictionary<long, string>();
            int minute = 60;
            int hour = 60 * minute;
            int day = 24 * hour;
            thresholds.Add(60, "{0} seconds ago");
            thresholds.Add(minute * 2, "a minute ago");
            thresholds.Add(45 * minute, "{0} minutes ago");
            thresholds.Add(120 * minute, "an hour ago");
            thresholds.Add(day, "{0} hours ago");
            thresholds.Add(day * 2, "yesterday");
            thresholds.Add(day * 30, "{0} days ago");
            thresholds.Add(day * 365, "{0} months ago");
            thresholds.Add(long.MaxValue, "{0} years ago");

            long since = (start.Ticks - input.Ticks) / 10000000;
            foreach (long threshold in thresholds.Keys)
            {
                if (since < threshold)
                {
                    TimeSpan t = new TimeSpan((start.Ticks - input.Ticks));
                    return string.Format(thresholds[threshold], (t.Days > 365 ? t.Days / 365 : (t.Days > 0 ? t.Days : (t.Hours > 0 ? t.Hours : (t.Minutes > 0 ? t.Minutes : (t.Seconds > 0 ? t.Seconds : 0))))).ToString());
                }
            }
            return "";
        }
    }
}
