using System.Text.RegularExpressions;
using System.Windows;

namespace csModels.PathPlanner
{
    public static class Extensions
    {
        //private const double Rad2Degrees = 180 / Math.PI;

        /// <summary>
        /// Convert a vection into an angle in degrees (0 degrees is looking North, 
        /// 360 degrees clockwise).
        /// </summary>
        /// <param name="vector"></param>
        /// <returns>Angle in degrees [0..360).</returns>
        public static double ToAngleInDegrees(this Vector vector)
        {
            return Vector.AngleBetween(vector, new Vector(0, 1));
        }

        public static string ToSentenceCase(this string str)
        {
            return Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + " " + char.ToLower(m.Value[1]));
        }
    }
}