using System;
using System.Text;

namespace csCommon.Utils
{
    public class StripGuid
    {
        public static string Strip(string input, char separator)
        {
            return Strip(input, separator, separator);
        }

        public static string Strip(string input, char separator, char newSeparator)
        {
            if (input.StartsWith("~"))
            {
                input = input.Substring(1);
            }
            StringBuilder sb = new StringBuilder();
            string[] split = input.Split(separator);
            bool first = true;
            foreach (string s in split)
            {
                Guid guid;
                if (!Guid.TryParse(s, out guid))
                {
                    if (!first)
                    {
                        sb.Append(newSeparator);
                    }
                    sb.Append(s);
                    first = false;
                }
            }
            return sb.ToString();

        }
    }
}
