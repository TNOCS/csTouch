// Copied from http://www.arcgis.com/home/item.html?id=1e432da7e74f4402bd43a5863167022d
using System.Windows;
using System.Windows.Shapes;

namespace csCommon.Types.Geometries.AdvancedGeometry
{
    /// <summary>
    /// Manages the attached property listening to the geometry changes in order to apply a geometry transformer.
    /// </summary>
    public static class GeometryTransformerService
    {
        #region Attached property GeometryTransformer
        /// <summary>
        /// Gets the geometry transformer.
        /// </summary>
        /// <param name="obj">The path object.</param>
        /// <returns></returns>
        public static IGeometryTransformer GetAttachedGeometryTransformer(DependencyObject obj)
        {
            return (IGeometryTransformer)obj.GetValue(AttachedGeometryTransformerProperty);
        }

        /// <summary>
        /// Sets the geometry transformer.
        /// </summary>
        /// <param name="obj">The path object.</param>
        /// <param name="value">The geometry transformer.</param>
        public static void SetAttachedGeometryTransformer(DependencyObject obj, IGeometryTransformer value)
        {
            obj.SetValue(AttachedGeometryTransformerProperty, value);
        }

        /// <summary>
        /// Identifies the GeometryTransformer attached property.
        /// </summary>
        public static readonly DependencyProperty AttachedGeometryTransformerProperty =
                DependencyProperty.RegisterAttached("AttachedGeometryTransformer", typeof(IGeometryTransformer), typeof(GeometryTransformerService), new PropertyMetadata(null, OnGeometryTransformerChanged));

        private static readonly DependencyProperty _geometryChangeListenerProperty =
                DependencyProperty.RegisterAttached("GeometryChangeListener", typeof(GeometryChangeListener), typeof(GeometryTransformerService), null);


        /// <summary>
        /// Called when the geometry transformer changed.
        /// Hook up a geometry transformer handler to the geometry changed event so the transformer will be executed each time the geometry of the path changes.
        /// </summary>
        /// <param name="d">The path.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnGeometryTransformerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var path = d as Path;
            if (path == null)
                return;

            var geometryTransformer = e.NewValue as IGeometryTransformer;

            // Find the geometry listener or create a new one
            var geometryChangeListener = path.GetValue(_geometryChangeListenerProperty) as GeometryChangeListener;
            if (geometryChangeListener == null)
            {
                geometryChangeListener = new GeometryChangeListener(path);
                path.SetValue(_geometryChangeListenerProperty, geometryChangeListener); // store it in private attached property in order to be able to unsubscribe
            }
            else
                geometryChangeListener.StopListening();

            if (geometryTransformer != null)
                geometryChangeListener.GeometryChanged += (s, evt) => geometryTransformer.Transform(evt.Geometry);
        }

        #endregion

    }
}
