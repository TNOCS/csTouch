using System;
using System.Globalization;
using System.Windows.Media;

namespace DataServer
{
    public class Ellipse
    {
        #region c'tors

        public Ellipse()
        {
        }

        public Ellipse(double majorAxis, double minorAxis)
        {
            MajorAxis = majorAxis;
            MinorAxis = minorAxis;
            FillColorString = "50,255,0,0";
        }

        public Ellipse(string ellipse)
        {
            var e = FromString(ellipse);
            MajorAxis = e.MajorAxis;
            MinorAxis = e.MinorAxis;
            Orientation = e.Orientation;
            FillColorString = e.FillColorString;
        }

        public Ellipse(double majorAxis, double minorAxis, double orientation)
        {
            MajorAxis = majorAxis;
            MinorAxis = minorAxis;
            Orientation = orientation;
            FillColorString = "50,255,0,0";
        }

        public Ellipse(double majorAxis, double minorAxis, double orientation, string fillColor)
        {
            MajorAxis = majorAxis;
            MinorAxis = minorAxis;
            Orientation = orientation;
            FillColorString = fillColor;
        }

        #endregion c'tors

        public double MajorAxis { get; set; }
        public double MinorAxis { get; set; }
        public double Orientation { get; set; }

        /// <summary>
        /// ARGB color representation, where the ARGB is represented in byte values [0 255], 
        /// e.g 50,255,0,0 is solid red at 50% opacity.
        /// </summary>
        public string FillColorString { get; set; }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.0};{1:0.0};{2:0.0};{3}", MajorAxis, MinorAxis, Orientation, FillColorString);
        }

        /// <summary>
        /// Create an Ellipse object from a string
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static Ellipse FromString(string f)
        {
            var ellipse = new Ellipse();
            try
            {
                var split = f.Split(new[] { ';' }, StringSplitOptions.None);
                if (split.Length != 4) return null;
                double result;
                if (double.TryParse(split[0], NumberStyles.Number, CultureInfo.InvariantCulture, out result)) ellipse.MajorAxis = result;
                if (double.TryParse(split[1], NumberStyles.Number, CultureInfo.InvariantCulture, out result)) ellipse.MinorAxis = result;
                if (double.TryParse(split[2], NumberStyles.Number, CultureInfo.InvariantCulture, out result)) ellipse.Orientation = result;
                ellipse.FillColorString = split[3];
                return ellipse;
            }
            catch
            {
                return null;
            }
        }

        public Color FillColor
        {
            get
            {
                try
                {
                    byte a,
                         r,
                         g,
                         b;
                    var c = new Color();
                    var split = FillColorString.Split(new[] { ',' }, StringSplitOptions.None);
                    if (byte.TryParse(split[0], NumberStyles.Number, CultureInfo.InvariantCulture, out a)) c.A = a;
                    if (byte.TryParse(split[1], NumberStyles.Number, CultureInfo.InvariantCulture, out r)) c.R = r;
                    if (byte.TryParse(split[2], NumberStyles.Number, CultureInfo.InvariantCulture, out g)) c.G = g;
                    if (byte.TryParse(split[3], NumberStyles.Number, CultureInfo.InvariantCulture, out b)) c.B = b;
                    return c;
                }
                catch
                {
                    return new Color();
                }
            }
        }
    }
}