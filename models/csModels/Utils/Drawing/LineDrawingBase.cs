using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using DataServer;
using ESRI.ArcGIS.Client;
using csShared;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using ESRI.ArcGIS.Client.Symbols;

namespace csModels.Utils.Drawing
{
    public delegate void DrawingCompleted(DrawingCompletedEventArgs args);
 
    public class LineDrawingBase
    {
        protected static readonly AppStateSettings AppState = AppStateSettings.Instance;
        protected static readonly WebMercator WebMercator = new WebMercator();

        protected NotificationEventArgs EditNotification;
        protected readonly PoI Poi;
        protected int InsertOrAppend;

        private Draw draw;
        private bool removeLastPoint;

        protected LineDrawingBase(PoI poi)
        {
            Poi = poi;
            EditNotification = new NotificationEventArgs
            {
                Id         = Guid.NewGuid(),
                Background = AppState.AccentBrush,
                Foreground = Brushes.White,
                Duration   = TimeSpan.FromDays(1),
                Options    = new List<string> { "DONE" }
            };
        }

        public event DrawingCompleted DrawingCompleted;

        /// <summary>
        /// Start drawing
        /// </summary>
        /// <param name="drawMode">Drawing mode</param>
        /// <param name="startPosition">Location where you wish to start the drawing, in WGS84</param>
        /// <param name="strokeColor">Stroke color</param>
        /// <param name="strokeWidth">Stroke width</param>
        protected void StartDrawing(DrawMode drawMode, Position startPosition, Color strokeColor, double strokeWidth = 2.0)
        {
            draw = new Draw(AppState.ViewDef.MapControl)
            {
                DrawMode   = drawMode,
                IsEnabled  = true,
                LineSymbol = new LineSymbol
                {
                    Width = strokeWidth,
                    Color = new SolidColorBrush(strokeColor)
                }
            };

            EditNotification.OptionClicked += (sender, args) =>
            {
                removeLastPoint = !args.UsesTouch; // Only remove the last point when the mouse was used.
                draw.CompleteDraw();
            };

            // Add the first point (drop point)
            draw.AddVertex(WebMercator.FromGeographic(startPosition.ToMapPoint()) as MapPoint);

            draw.DrawComplete += OnDrawingCompleted;
        }

        private void OnDrawingCompleted(object sender, DrawEventArgs e)
        {
            AppState.TriggerDeleteNotification(EditNotification);

            draw.IsEnabled = false;
            var pl = e.Geometry as Polyline;
            if (pl == null) return;
            var drawingCompleted = new DrawingCompletedEventArgs
            {
                Points = pl.Paths[0].Select(p => (MapPoint) WebMercator.ToGeographic(new MapPoint(p.X, p.Y))).Select(convertedPoint => new Point(convertedPoint.X, convertedPoint.Y)).ToList()
            };

            if (removeLastPoint) drawingCompleted.Points.RemoveAt(drawingCompleted.Points.Count - 1);

            OnDrawingCompleted(drawingCompleted);
        }

        private void OnDrawingCompleted(DrawingCompletedEventArgs args)
        {
            var handler = DrawingCompleted;
            if (handler != null) handler(args);
        }

    }


    public class DrawingCompletedEventArgs
    {
        /// <summary>
        /// List of points in WGS84 that are part of the line.
        /// </summary>
        public List<Point> Points { get; set; }
    }
}
