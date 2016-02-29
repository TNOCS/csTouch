using System.Windows.Media;
using DataServer;
using ESRI.ArcGIS.Client;

namespace csModels.Utils.Drawing
{
    /// <summary>
    /// Draw a line, freehand style.
    /// </summary>
    public class DrawFreehand : LineDrawingBase
    {
        /// <summary>
        /// Create an object to draw a freehand line
        /// </summary>
        /// <param name="poi"></param>
        public DrawFreehand(PoI poi)
            : base(poi)
        {
        }

        /// <summary>
        /// Start drawing the line.
        /// </summary>
        /// <param name="strokeColor"></param>
        /// <param name="insertOrAppend">Insert or append the line to the Poi. In case the value is less than 0, append, otherwise insert.</param>
        /// <param name="strokeWidth"></param>
        public void StartDrawing(Color strokeColor, int insertOrAppend = -1, double strokeWidth = 2.0)
        {
            InsertOrAppend = insertOrAppend;
            var startPoint = insertOrAppend > 0 && insertOrAppend < Poi.Points.Count
                ? new Position(Poi.Points[insertOrAppend].X, Poi.Points[insertOrAppend].Y)
                : Poi.Position;

            StartDrawing(DrawMode.Freehand, startPoint, strokeColor, strokeWidth);

            EditNotification.Header = "Edit";
            EditNotification.Text   = "Draw the line.";

            DrawingCompleted += FreehandDrawingCompleted;
        }

        private void FreehandDrawingCompleted(DrawingCompletedEventArgs args)
        {
            var i = InsertOrAppend < 0
                ? Poi.Points.Count
                : InsertOrAppend;

            for (var j = 0; j < args.Points.Count; j++)
                Poi.Points.Insert(i + j, args.Points[j]);
        }
    }
}
