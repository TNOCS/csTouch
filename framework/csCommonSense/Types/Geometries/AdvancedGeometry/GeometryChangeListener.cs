// Copied from http://www.arcgis.com/home/item.html?id=1e432da7e74f4402bd43a5863167022d
using System;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace csCommon.Types.Geometries.AdvancedGeometry
{
    /// <summary>
    /// Listen to the geometry changes of a path object.
    /// </summary>
    public class GeometryChangeListener
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="GeometryChangeListener"/> class and creates the binding with the geometry of the path.
        /// </summary>
        /// <param name="path">The path.</param>
        public GeometryChangeListener(Path path)
        {
            var binding = new Binding("Geometry") { Mode = BindingMode.TwoWay, Source = this }; // might be OneWayToSource with WPF
            path.SetBinding(Path.DataProperty, binding);
        }
        #endregion

        #region Geometry
        private Geometry _geometry;
        /// <summary>
        /// Gets or sets the geometry used to listen to the path geometry changes.
        /// </summary>
        /// <value>The geometry.</value>
        public Geometry Geometry
        {
            get { return _geometry; }
            set
            {
                if ((_geometry == null) || (!_geometry.Equals(value)))
                {
                    _geometry = value;
                    OnGeometryChanged(this, new GeometryEventsArgs {Geometry = _geometry});
                }
            }
        }
        #endregion

        #region GeometryChanged event
        /// <summary>
        /// Occurs when the geometry changed.
        /// </summary>
        public event EventHandler<GeometryEventsArgs> GeometryChanged;
        private void OnGeometryChanged(object sender, GeometryEventsArgs e)
        {
            var geometryChanged = GeometryChanged;
            if (geometryChanged != null)
                geometryChanged(sender, e);
        }
        #endregion

        #region StopListening
        /// <summary>
        /// Stops the listening.
        /// </summary>
        public void StopListening()
        {
            // Lazy way to unsubscribe (I am ashamed of) :-)
            GeometryChanged = null;
        }
        #endregion

        #region GeometryEventsArgs class
        /// <summary>
        /// Class containing event geometry data.
        /// </summary>
        public class GeometryEventsArgs : EventArgs
        {
            /// <summary>
            /// Gets or sets the geometry.
            /// </summary>
            /// <value>The geometry.</value>
            public Geometry Geometry { get; internal set; }
        }
        #endregion

    }
}
