using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace csCommon.Types.Geometries.AdvancedGeometry
{
    public interface IDrawAdvancedGeometry
    {
        Double AdvancedLineStrokeWidth { get; set; }
        Brush AdvancedLineStroke { get; set; }
        Brush AdvancedFillStroke { get; set; }
        IGeometryTransformer AdvancedLineGeometryTransformer { get; set; }
        DoubleCollection AdvancedLineDash { get; set; }
    }
        
}
