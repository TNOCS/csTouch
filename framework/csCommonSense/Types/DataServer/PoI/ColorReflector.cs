using System;
using System.Globalization;
using System.Reflection;
using System.Windows.Media;

namespace DataServer
{
    public static class ColorReflector
    {
        public static void SetFromHex(this Color c, string hex) {
            var c1 = ToColorFromHex(hex);

            c.A = c1.A;
            c.R = c1.R;
            c.G = c1.G;
            c.B = c1.B;
        }


        public static Color GetThisColor(string colorString) {
            var colorType = (typeof (Colors));
            if (colorType.GetProperty(colorString) != null) {
                var o = colorType.InvokeMember(colorString, BindingFlags.GetProperty, null, null, null);
                if (o != null) {
                    return (Color) o;
                }
            }
            return Colors.Black;
        }


        public static Color ToColorFromHex(string hex) {
            if (string.IsNullOrEmpty(hex))
            {
                return Colors.Transparent;
            }

            // remove any "#" characters
            while (hex.StartsWith("#")) {
                hex = hex.Substring(1);
            }

            var num = 0;
            // get the number out of the string 
            if (!Int32.TryParse(hex, NumberStyles.HexNumber, null, out num)) {
                return GetThisColor(hex);
            }

            var pieces = new int[4];
            if (hex.Length > 7) {
                pieces[0] = ((num >> 24) & 0x000000ff);
                pieces[1] = ((num >> 16) & 0x000000ff);
                pieces[2] = ((num >> 8) & 0x000000ff);
                pieces[3] = (num & 0x000000ff);
            }
            else if (hex.Length > 5) {
                pieces[0] = 255;
                pieces[1] = ((num >> 16) & 0x000000ff);
                pieces[2] = ((num >> 8) & 0x000000ff);
                pieces[3] = (num & 0x000000ff);
            }
            else if (hex.Length == 3) {
                pieces[0] = 255;
                pieces[1] = ((num >> 8) & 0x0000000f);
                pieces[1] += pieces[1]*16;
                pieces[2] = ((num >> 4) & 0x000000f);
                pieces[2] += pieces[2]*16;
                pieces[3] = (num & 0x000000f);
                pieces[3] += pieces[3]*16;
            }
            return Color.FromArgb((byte) pieces[0], (byte) pieces[1], (byte) pieces[2], (byte) pieces[3]);
        }
    }
}