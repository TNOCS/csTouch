using Caliburn.Micro;
using csDataServerPlugin;
using DataServer;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using ESRI.ArcGIS.Client.Symbols;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Media;
using PointCollection = ESRI.ArcGIS.Client.Geometry.PointCollection;

namespace csModels.TrackHistory
{
    /// <summary>
    /// A PoI Model that creates a track history behind a PoI.
    /// 
    /// Input labels:
    ///     * ModelId.TrackHistoryLength (int)
    ///     * ModelId.TrackColor (string)
    ///     * ModelId.TrackWidth (int)
    ///     * ModelId.HighContrast (boolean)
    ///     * ModelId.ShowArrow (boolean)
    /// 
    /// </summary>
    public class TrackHistoryPoi : ModelPoiBase
    {
        private const int DefaultTrackHistoryLength = 15;
        private const int DefaultTrackWidth = 2;
        private const string DefaultTrackColor = "Blue";

        //private static readonly AppStateSettings AppState = AppStateSettings.Instance;
        private static readonly WebMercator WebMercator = new WebMercator();

        /// <summary>
        /// A historic record of the last 'trackHistoryLength' positions.
        /// </summary>
        private readonly PointCollection trackCoordinates = new PointCollection();

        /// <summary>
        /// In case the user wishes to show an arrow with the direction.
        /// </summary>
        private readonly PointCollection arrowCoordinates = new PointCollection();

        /// <summary>
        /// Specifies the number of points that you wish to remember.
        /// </summary>
        private int TrackHistoryLength { get; set; }

        /// <summary>
        /// Color of the track line.
        /// </summary>
        private Color TrackColor { get; set; }

        /// <summary>
        /// Stroke width of the track line.
        /// </summary>
        private int TrackWidth { get; set; }

        /// <summary>
        /// Specify whether the track needs to be displayed using a contrasting background color
        /// </summary>
        private bool HighContrast { get; set; }

        /// <summary>
        /// Show a direction arrow (where you are probably going).
        /// </summary>
        private bool ShowArrow { get; set; }

        /// <summary>
        /// Show a dragging trail (where you have been).
        /// </summary>
        private bool ShowTrail { get; set; }

        public GraphicsLayer GraphicsLayer { private get; set; }

        public override void Start()
        {
            base.Start();

            UpdateInfoFromLabels();

            var posChanged = Observable.FromEventPattern<PositionEventArgs>(ev => Poi.PositionChanged += ev, ev => Poi.PositionChanged -= ev);
            posChanged.Throttle(TimeSpan.FromMilliseconds(500)).Subscribe(k => RedrawTrackHistory());

            var dataChanged = Observable.FromEventPattern<LabelChangedEventArgs>(ev => Poi.DataChanged += ev, ev => Poi.DataChanged -= ev);
            dataChanged.Throttle(TimeSpan.FromMilliseconds(250)).Subscribe(ProcessLabels);

            var labelChanged = Observable.FromEventPattern<LabelChangedEventArgs>(ev => Poi.LabelChanged += ev, ev => Poi.LabelChanged -= ev);
            labelChanged.Throttle(TimeSpan.FromMilliseconds(250)).Subscribe(ProcessLabels);
        }

        public override void Stop() {
            base.Stop(); 
            Execute.OnUIThread(ClearGraphics);
        }

        /// <summary>
        /// Obtain the track history parameters from the labels.
        /// </summary>
        /// <param name="e"></param>
        private void ProcessLabels(EventPattern<LabelChangedEventArgs> e)
        {
            var label = e.EventArgs.Label;

            if (!(string.IsNullOrEmpty(label) 
                || label.StartsWith("Data." + Model.Id) 
                || string.Equals(label, "UserIdentity", StringComparison.InvariantCultureIgnoreCase))) return;
            UpdateInfoFromLabels();
        }

        /// <summary>
        /// Update the internal state based on the label values. 
        /// </summary>
        public void UpdateInfoFromLabels()
        {
            var label = "Data." + Model.Id + ".TrackHistoryLength";
            if (!Poi.Data.ContainsKey(label)) Poi.Data[label] = DefaultTrackHistoryLength.ToString(CultureInfo.InvariantCulture);
            TrackHistoryLength = Model.Model.GetInt(Poi.Data[label].ToString(), DefaultTrackHistoryLength);

            label = "Data." + Model.Id + ".TrackColor";
            if (!Poi.Data.ContainsKey(label)) Poi.Data[label] = DefaultTrackColor;
            TrackColor = Model.Model.GetColorFromName(Poi.Data[label].ToString(), Colors.Blue);

            label = "Data." + Model.Id + ".TrackWidth";
            if (!Poi.Data.ContainsKey(label)) Poi.Data[label] = DefaultTrackWidth.ToString(CultureInfo.InvariantCulture);
            TrackWidth = Model.Model.GetInt(Poi.Data[label].ToString(), DefaultTrackWidth);

            label = "Data." + Model.Id + ".TrackContrast";
            if (!Poi.Data.ContainsKey(label)) Poi.Data[label] = "true";
            HighContrast = Model.Model.GetBoolean(Poi.Data[label].ToString(), true);

            label = "Data." + Model.Id + ".ShowArrow";
            if (!Poi.Data.ContainsKey(label)) Poi.Data[label] = "false";
            ShowArrow = Model.Model.GetBoolean(Poi.Data[label].ToString(), false);

            label = "Data." + Model.Id + ".ShowTrail";
            if (!Poi.Data.ContainsKey(label)) Poi.Data[label] = "false";
            ShowTrail = Model.Model.GetBoolean(Poi.Data[label].ToString(), false);

            #region Special case for COPPR

            if (Poi.Labels.ContainsKey("UserIdentity"))
            {
                var color = ConvertIdentityToColor(Poi.Labels["UserIdentity"]);
                Poi.Data["Data." + Model.Id + ".TrackColor"] = color;
                switch (color)
                {
                    case "yellow": TrackColor = Colors.Yellow; break;
                    case "blue"  : TrackColor = Colors.Blue; break;
                    case "red"   : TrackColor = Colors.Red; break;
                    case "orange": TrackColor = Colors.Orange; break;
                    case "green" : TrackColor = Colors.Green; break;
                    case "white" : TrackColor = Colors.White; break;
                }
            }

            #endregion Special case for COPPR

            RedrawTrackHistory(false);
        }

        private static string ConvertIdentityToColor(string identity) {
            if (string.IsNullOrEmpty(identity)) return "yellow";
            identity = identity.ToLower();
            if (identity.Contains("unknown")) return "yellow";
            if (identity.Contains("friend" )) return "blue";
            if (identity.Contains("suspect")) return "orange";
            if (identity.Contains("hostile")) return "red";
            if (identity.Contains("neutral")) return "green";
            if (identity.Contains("pending")) return "white";
            return "yellow";
        }

        private readonly object tracksLock = new object();

        /// <summary>
        /// Draw the track's trail and future direction (arrow).
        /// </summary>
        /// <param name="updateCoordinates">If true (default), update the coordinates</param>
        private void RedrawTrackHistory(bool updateCoordinates = true)
        {
            if (Poi == null || Poi.Position == null || IsStopping ||
                (Poi.Position.Longitude.IsZero() && Poi.Position.Latitude.IsZero())) return;
            if (updateCoordinates)
            {
                lock (tracksLock)
                {
                    if (trackCoordinates.Count > TrackHistoryLength)
                    {
                        // TODO REVIEW
                        trackCoordinates.RemoveAt(0);
                    }
                    trackCoordinates.Add((MapPoint) WebMercator.FromGeographic(new MapPoint(Poi.Position.Longitude, Poi.Position.Latitude)));
                }
            }
            if (trackCoordinates.Count < 2) return;
            if (ShowArrow) CreateArrow();

            Execute.OnUIThread(() =>
            {
                ClearGraphics();
                if (ShowTrail) DrawLine(trackCoordinates);
                if (ShowArrow) DrawArrow(arrowCoordinates);
            });
        }

        private readonly object myLock = new object();

        /// <summary>
        /// Create a line, representing an arrow. The result is added to the arrowCoordinates collection.
        /// NOTE: The first two points represent the line, the other three represent the arrow head itself.
        /// </summary>
        private void CreateArrow()
        {
            var scaleFactor = 2D;             // The scale factor is used to grow the arrow (make it bigger).
            var length = trackCoordinates.Count;
            var p1 = trackCoordinates[length - 1]; // Start point of the arrow, is the last point of the track history.
            var p2 = trackCoordinates[0];          // Reference point is the one but last point of the track history.
            var dx = p1.X - p2.X;
            var dy = p1.Y - p2.Y;
            var arrowLength = Math.Max(Math.Abs(dx), Math.Abs(dy));
            if (arrowLength < 200) scaleFactor = 200 / arrowLength;
            else if (arrowLength > 1000) scaleFactor = 1000 / arrowLength;
            var p3 = new MapPoint(p1.X + dx * scaleFactor, p1.Y + dy * scaleFactor); // End point of the arrow
            var p4 = new MapPoint(p1.X + dx * 0.8 * scaleFactor, p1.Y + dy * 0.8 * scaleFactor); // Cross-section point of the arrow with line
            dx *= scaleFactor / 10;
            dy *= scaleFactor / 10;
            var p5 = new MapPoint(p4.X - dy, p4.Y + dx); // Corner point of the arrow
            var p6 = new MapPoint(p4.X + dy, p4.Y - dx); // Corner point of the arrow
            lock (myLock)
            {
                arrowCoordinates.Clear();
                arrowCoordinates.Add(p1); // Start point of line            
                arrowCoordinates.Add(p4); // End point of line
                arrowCoordinates.Add(p6); // 3 point of arrow head
                arrowCoordinates.Add(p5);
                arrowCoordinates.Add(p3);
            }
        }

        private void DrawLine(PointCollection coordinates)
        {
            var pl = new Polyline();
            pl.Paths.Add(coordinates);
            if (HighContrast)
            {
                var g = new Graphic
                {
                    Symbol = new SimpleLineSymbol
                    {
                        Color = Brushes.White,
                        Width = TrackWidth + 4,
                        Style = SimpleLineSymbol.LineStyle.Solid
                    },
                    Geometry = pl
                };
                g.Attributes["ID"] = Poi.Id.ToString();
                GraphicsLayer.Graphics.Add(g);
            }
            var g2 = new Graphic
            {
                Symbol = new SimpleLineSymbol
                {
                    Color = new SolidColorBrush(TrackColor),
                    Width = TrackWidth,
                    Style = SimpleLineSymbol.LineStyle.Solid
                },
                Geometry = pl
            };
            g2.Attributes["ID"] = Poi.Id.ToString();
            GraphicsLayer.Graphics.Add(g2);
        }

        private void DrawArrow(IReadOnlyList<MapPoint> coordinates)
        {
            // Draw arrow line using the first two coordinates
            DrawLine(new PointCollection { coordinates[0], coordinates[1] });
            // The other three points define the arrow head
            var pl = new Polygon();
            pl.Rings.Add(new PointCollection { coordinates[2], coordinates[3], coordinates[4] });
            var g2 = new Graphic
            {
                Symbol = new SimpleFillSymbol
                {
                    BorderBrush = Brushes.White,
                    BorderThickness = 2,
                    Fill = new SolidColorBrush(TrackColor),
                },
                Geometry = pl
            };
            g2.Attributes["ID"] = Poi.Id.ToString();
            GraphicsLayer.Graphics.Add(g2);
        }

        private void ClearGraphics()
        {
            var id = Poi.Id.ToString();
            foreach (var graphic in GraphicsLayer.Graphics.Where(g => string.Equals(id, g.Attributes["ID"].ToString(), StringComparison.InvariantCultureIgnoreCase)).ToList())
                GraphicsLayer.Graphics.Remove(graphic);
        }

    }
}
