using System;
using System.Globalization;
using System.Windows.Media.Media3D;

namespace csImb
{
    public class Pos3D : Message
    {
        public Point3D Camera { get; set; }
        public Point3D Destination { get; set; }

        public override string ToString()
        {
            return this.Camera.X.ToString(CultureInfo.InvariantCulture) + "|" +
                   this.Camera.Y.ToString(CultureInfo.InvariantCulture) + "|" +
                   this.Camera.Z.ToString(CultureInfo.InvariantCulture) + "|" +
                   this.Destination.X.ToString(CultureInfo.InvariantCulture) + "|" +
                   this.Destination.Y.ToString(CultureInfo.InvariantCulture) + "|" +
                   this.Destination.Z.ToString(CultureInfo.InvariantCulture);

        }

        public static Pos3D FromString(string value)
        {
            try
            {
                var s = value.Split('|');
                var result = new Pos3D();
                result.Camera = new Point3D(Convert.ToDouble(s[0], CultureInfo.InvariantCulture), Convert.ToDouble(s[1], CultureInfo.InvariantCulture),  Convert.ToDouble(s[2], CultureInfo.InvariantCulture));
                result.Destination = new Point3D(Convert.ToDouble(s[3], CultureInfo.InvariantCulture), Convert.ToDouble(s[4], CultureInfo.InvariantCulture), Convert.ToDouble(s[5], CultureInfo.InvariantCulture));
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error parsing command:" + e.Message);
                return null;
            }
        }

    }
}
