using System;

namespace DataServer
{
    public enum DrawingModes
    {
        /// <summary>
        /// Default value when not set. This means not to draw.
        /// </summary>
        None,
        /// <summary>
        /// Draws an image, as specified by the object having this drawing mode.
        /// </summary>
        Image,
        /// <summary>
        /// Draws an overlay layer, as specified by the object having this drawing mode.
        /// </summary>
        ImageOverlay,
        /// <summary>
        /// Draws a geometry, as specified by the object having this drawing mode.
        /// </summary>
        Geometry,
        [Obsolete("Using DrawingModes to influence the way a geometry is drawn is deprecated. " +
                  "Drawing mode should be derived from the Geometry property of the PoI. " +
                  "Having a separate DrawingMode creates inconsistencies between the Geometry and " +
                  "the way to draw it. Use DrawingModes.Geometry to indicate the shape to be " +
                  "drawn must be derived from the actual geometry.")]
        Point,
        [Obsolete("Using DrawingModes to influence the way a geometry is drawn is deprecated. " +
                  "Drawing mode should be derived from the Geometry property of the PoI. " +
                  "Having a separate DrawingMode creates inconsistencies between the Geometry and " +
                  "the way to draw it. Use DrawingModes.Geometry to indicate the shape to be " +
                  "drawn must be derived from the actual geometry.")]
        Square,
        [Obsolete("Using DrawingModes to influence the way a geometry is drawn is deprecated. " +
                  "Drawing mode should be derived from the Geometry property of the PoI. " +
                  "Having a separate DrawingMode creates inconsistencies between the Geometry and " +
                  "the way to draw it. Use DrawingModes.Geometry to indicate the shape to be " +
                  "drawn must be derived from the actual geometry.")]
        Rectangle,
        [Obsolete("Using DrawingModes to influence the way a geometry is drawn is deprecated. " +
                  "Drawing mode should be derived from the Geometry property of the PoI. " +
                  "Having a separate DrawingMode creates inconsistencies between the Geometry and " +
                  "the way to draw it. Use DrawingModes.Geometry to indicate the shape to be " +
                  "drawn must be derived from the actual geometry.")]
        Line,
        [Obsolete("Using DrawingModes to influence the way a geometry is drawn is deprecated. " +
                  "Drawing mode should be derived from the Geometry property of the PoI. " +
                  "Having a separate DrawingMode creates inconsistencies between the Geometry and " +
                  "the way to draw it. Use DrawingModes.Geometry to indicate the shape to be " +
                  "drawn must be derived from the actual geometry.")]
        Circle,
        [Obsolete("Using DrawingModes to influence the way a geometry is drawn is deprecated. " +
                  "Drawing mode should be derived from the Geometry property of the PoI. " +
                  "Having a separate DrawingMode creates inconsistencies between the Geometry and " +
                  "the way to draw it. Use DrawingModes.Geometry to indicate the shape to be " +
                  "drawn must be derived from the actual geometry.")]
        Freehand,
        [Obsolete("Using DrawingModes to influence the way a geometry is drawn is deprecated. " +
                  "Drawing mode should be derived from the Geometry property of the PoI. " +
                  "Having a separate DrawingMode creates inconsistencies between the Geometry and " +
                  "the way to draw it. Use DrawingModes.Geometry to indicate the shape to be " +
                  "drawn must be derived from the actual geometry.")]
        Polyline,

        AdvancedPolyline,
        [Obsolete("Using DrawingModes to influence the way a geometry is drawn is deprecated. " +
                  "Drawing mode should be derived from the Geometry property of the PoI. " +
                  "Having a separate DrawingMode creates inconsistencies between the Geometry and " +
                  "the way to draw it. Use DrawingModes.Geometry to indicate the shape to be " +
                  "drawn must be derived from the actual geometry.")]
        Polygon,
        [Obsolete("Using DrawingModes to influence the way a geometry is drawn is deprecated. " +
                  "Drawing mode should be derived from the Geometry property of the PoI. " +
                  "Having a separate DrawingMode creates inconsistencies between the Geometry and " +
                  "the way to draw it. Use DrawingModes.Geometry to indicate the shape to be " +
                  "drawn must be derived from the actual geometry.")]
        MultiPolygon
    }
}