using System.Windows.Media;

namespace DataServer
{
    public class BoundaryColors
    {
        public Color LowerColor { get; set; }
        public Color UpperColor { get; set; }
        public Color CenterColor { get; set; }
        public LinearGradientBrush Gradient { get; set; }
        public double UpperBound { get; set; }
        public double LowerBound { get; set; }
    }
}