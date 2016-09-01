using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using csModels.PathEditor;
using csModels.Utils.Drawing;
using csShared.Controls.Popups.MapCallOut;
using DataServer;
using ESRI.ArcGIS.Client.Projection;
using Humanizer;

namespace csModels.Router
{
    /// <summary>
    /// The router view model
    /// </summary>
    [Export(typeof(IScreen))]
    public class RouterViewModel : Screen, IEditableScreen
    {
        private const string PoiInnerTextLabel  = "InnerText";
        private const string ArrivalTimeLabel   = "ArrivalTime";
        private const string DepartureTimeLabel = "DepartureTime";

        private static readonly WebMercator WebMercator = new WebMercator();
        private List<ModelParameter> modelParameters;
        private bool continuePathFromLastPoint = true;

        public RouterViewModel()
        {
            WayPoints = new BindableCollection<PoI>();
        }

        public BindableCollection<PoI> WayPoints { get; set; }

        public IModel Model { private get; set; }

        public PoI Poi { private get; set; }

        #region Drawing functions

        public bool ContinuePathFromLastPoint {
            get { return continuePathFromLastPoint; }
            set {
                if (value.Equals(continuePathFromLastPoint)) return;
                continuePathFromLastPoint = value;
                NotifyOfPropertyChange(() => ContinuePathFromLastPoint);
            }
        }

        private PoI CreateWayPoint(Point pos, TrackPoiType trackPoiType)
        {
            var layer = Poi.Layer;
            if (!string.IsNullOrEmpty(layer))
            {
                var index = layer.IndexOf("_", StringComparison.InvariantCultureIgnoreCase);
                if (index > 0) layer = layer.Substring(0, index);
            }
            var poi = new PoI
            {
                Id        = Guid.NewGuid(),
                Name      = trackPoiType.Humanize(),
                Service   = Model.Service,
                ContentId = trackPoiType.ToString(),
                UserId    = Poi.Id.ToString(),
                Layer     = layer,
                Position  = new Position(pos.X, pos.Y),
                Style     = new DataServer.PoIStyle()
                {
                    DrawingMode       = DrawingModes.Point,
                    FillColor         = Colors.Gold,
                    StrokeColor       = Colors.Black,
                    CallOutFillColor  = Colors.White,
                    CallOutForeground = Colors.Black,
                    StrokeOpacity     = 1,
                    StrokeWidth       = 2,
                    InnerTextLabel    = PoiInnerTextLabel,
                    TitleMode         = TitleModes.Bottom,
                    TapMode           = TapMode.CallOutPopup,
                    InnerTextColor    = Colors.Black,
                    CanEdit           = false
                },
                MetaInfo              = new MetaInfoCollection
                {
                    new MetaInfo { IsEditable = false, Label = "Name", Title = "Name", Type = MetaTypes.text, VisibleInCallOut = true }
                }
            };

            switch (trackPoiType)
            {
                case TrackPoiType.Start:
                    poi.Labels[PoiInnerTextLabel] = "α";
                    break;
                case TrackPoiType.End:
                    poi.Labels[PoiInnerTextLabel] = "Ω";
                    break;
                case TrackPoiType.WayPoint:
                    poi.Labels[PoiInnerTextLabel] = "*";
                    break;
            }

            Execute.OnUIThread(() =>
            {
                poi.TriggerUpdated();
                Model.Service.PoIs.Add(poi);
            });

            return poi;
        }

        private PoI CreateSegment(IEnumerable<Point> positions)
        {
            var layer = Poi.Layer;
            if (!string.IsNullOrEmpty(layer))
            {
                var index = layer.IndexOf("_", StringComparison.InvariantCultureIgnoreCase);
                if (index > 0) layer = layer.Substring(0, index);
            }
            var poi = new PoI
            {
                Id        = Guid.NewGuid(),
                //Name      = trackPoiType.Humanize(),
                Service   = Model.Service,
                //ContentId = trackPoiType.ToString(),
                UserId    = Poi.Id.ToString(),
                Layer     = layer,
                Style     = new PoIStyle
                {
                    DrawingMode       = DrawingModes.Polyline,
                    StrokeColor       = Colors.Blue,
                    CallOutFillColor  = Colors.White,
                    CallOutForeground = Colors.Black,
                    StrokeOpacity     = 1,
                    StrokeWidth       = 4,
                    TitleMode         = TitleModes.Bottom,
                    TapMode           = TapMode.CallOutPopup,
                    CanEdit           = false
                },
                MetaInfo              = new MetaInfoCollection()
                {
                    new MetaInfo { IsEditable = false, Label = "Name", Title = "Name", Type = MetaTypes.text, VisibleInCallOut = true }
                }
            };
            foreach (var p in positions) poi.Points.Add(p);
            
            Execute.OnUIThread(() =>
            {
                poi.TriggerUpdated();
                Model.Service.PoIs.Add(poi);
            });

            return poi;
        }

        public void DrawPolylineRoute()
        {
            var draw = new DrawPolyline(Poi);
            draw.DrawingCompleted += args => {
                if (args.Points.Count == 0) return;
                if (!continuePathFromLastPoint)
                    CreateWayPoint(args.Points.First(), TrackPoiType.Start);
                CreateSegment(args.Points);
                CreateWayPoint(args.Points.Last(), TrackPoiType.End);
            };
            draw.StartDrawing(Colors.Blue, continuePathFromLastPoint && Poi.Points.Count > 0 ? Poi.Points.Count - 1 : -1);
        }

        public void DrawFreehandRoute()
        {
            var draw = new DrawFreehand(Poi);
            draw.DrawingCompleted += args => {
                if (args.Points.Count == 0) return;
                if (!continuePathFromLastPoint)
                    CreateWayPoint(args.Points.First(), TrackPoiType.Start);
                CreateSegment(args.Points);
                CreateWayPoint(args.Points.Last(), TrackPoiType.End);
            };
            draw.StartDrawing(Colors.Blue);            
        }

        public void DrawGoogleWalkingRoute()
        {
            
        }

        public void DrawGoogleDrivingRoute()
        {
            
        }

        #endregion Drawing functions

        public bool CanEdit { get; set; }

        public MapCallOutViewModel CallOut { get; set; }

        public void Initialize() {
            modelParameters = Model.Model.Parameters;
            
        }
    }
}