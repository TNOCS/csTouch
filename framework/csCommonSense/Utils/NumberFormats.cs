using System;
using System.Collections;
using System.Collections.Generic;

namespace csCommon.Utils
{
    /// <summary>
    ///     Enumerates the various formats for numbers (i.e. C# formatting templates). Provides convenience translation to a
    ///     meaningful description, e.g. for "percent" or "two decimals".
    /// </summary>
    public class NumberFormats : IEnumerable<string>
    {
        public const string UNFORMATTED = "Unformatted";
        public const string INTEGER = "Integer";

        private static readonly string[] formatDescriptionsA =
        {
            UNFORMATTED,
            INTEGER,
            "One decimal",
            "Max. one decimal",
            "Two decimals",
            "Max. two decimals",
            "Percentage",
            "Euro (no decimals)",
            "Million euro",
            "Percent"
        };

        private static readonly string[] formatsA =
        {
            null,
            "{0:0,0}",
            "{0:0,0.0}",
            "{0:0,0.#}",
            "{0:0,0.00}",
            "{0:0.0%}",
            "{0:0,0.##}",
            "€{0:0,0}",
            "\u20AC {#.##0,,M}", // TODO TEST!!!
            "{0}%"
        };


        private static readonly List<string> formatDescriptions = new List<string>(formatDescriptionsA);
        private static readonly List<string> formats = new List<string>(formatsA);

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return formatDescriptions.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return formatDescriptions.GetEnumerator();
        }

        public static string GetFormatDescription(string formatString)
        {
            int i = formats.IndexOf(formatString);
            if (i != -1)
            {
                return formatDescriptions[i];
            }
            return null;
        }

        public static string GetFormatString(string formatDescription)
        {
            int i = formatDescriptions.IndexOf(formatDescription);
            if (i != -1)
            {
                return formats[i];
            }
            return null;
        }

        public static bool IsUnformatted(string formatDescription)
        {
            return String.Equals(formatDescription, UNFORMATTED);
        }
    }
}