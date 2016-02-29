// Copied from http://www.arcgis.com/home/item.html?id=1e432da7e74f4402bd43a5863167022d
using System.Windows.Media;


namespace csCommon.Types.Geometries.AdvancedGeometry
{
    /// <summary>
    /// Interface geometry transformers must implement
    /// </summary>
    public interface IGeometryTransformer
    {
        /// <summary>
        /// Transforms the specified geometry.
        /// </summary>
        /// <param name="geometry">The geometry.</param>
        void Transform(Geometry geometry);
    }
}
