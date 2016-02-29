using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using Caliburn.Micro;
using csCommon.MapPlugins.MapTools.RouteTool;
using csDataServerPlugin;
using csShared;
using csShared.Utils;
using DataServer;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using ESRI.ArcGIS.Client.Symbols;
using Humanizer;
using Point = System.Windows.Point;
using Polyline = ESRI.ArcGIS.Client.Geometry.Polyline;
using Task = System.Threading.Tasks.Task;

namespace csModels.PathEditor
{
    public class PathEditorPoi : ModelPoiBase
    {
        private const string KeyLabelDistance      = "Distance";
        private const string KeyLabelTravelTime    = "TravelTime";
        private const string PathEditorPathTypeKey = "PathEditor.PathType";

        private readonly WebMercator webMercator = new WebMercator();

        private PathType selectedPathType = PathType.GoogleDrivingDirections;
        private Draw  draw;
        private Point start;
        private Point end;

        private Point lastTapPoint;
        private bool addingWayPoint;
        private bool removeLastPoint;

        private NotificationEventArgs notification;

        public override void Start()
        {
            base.Start();

            WayPoints = new BindableCollection<WayPoint>();

            if (Poi.Labels.ContainsKey(PathEditorPathTypeKey))
                Enum.TryParse(Poi.Labels[PathEditorPathTypeKey], true, out selectedPathType);
            else
                SelectedPathType = PathType.GoogleDrivingDirections;

            if (Poi.Service.IsInitialized)
            {
                Model.Service.Tapped += ServiceTapped;
                Poi.LabelChanged += (o, e) =>
                {
                    if (!string.Equals(e.Label, PathEditorPathTypeKey, StringComparison.InvariantCultureIgnoreCase))
                        return;
                    Enum.TryParse(Poi.Labels[PathEditorPathTypeKey], true, out selectedPathType);
                    DrawRoute();
                };
                if (Poi.Points.Count == 2)
                {
                    // PathPlanner created this path.
                    ResetPath();
                    DrawRoute();
                }
                else
                {
                    ChangePoiToPolyline();
                    CreateInitialPath();
                }
            }
            else
            {
                Poi.Service.Initialized += (sender, args) =>
                {
                    RestoreState();
                    Model.Service.Tapped += ServiceTapped;
                    Poi.LabelChanged += (o, e) =>
                    {
                        if (!string.Equals(e.Label, PathEditorPathTypeKey, StringComparison.InvariantCultureIgnoreCase))
                            return;
                        Enum.TryParse(Poi.Labels[PathEditorPathTypeKey], true, out selectedPathType);
                        DrawRoute();
                    };
                };
            }
        }

        private void ChangePoiToPolyline()
        {
            Execute.OnUIThread(() =>
            {
                Poi.Deleted += (sender, e) => OnPoiDeleted(Poi, TrackPoiType.Route);

                Poi.Style = Poi.NEffectiveStyle.Clone() as PoIStyle;
                if (Poi.Style != null)
                {
                    Poi.Style.Icon        = string.Empty;
                    Poi.Style.DrawingMode = DrawingModes.Polyline;
                    Poi.Style.FillColor   = Colors.Transparent;
                }
                Poi.Layer += "_path";
                start = new Point(Poi.Position.Longitude, Poi.Position.Latitude);
                Poi.Points.Add(start);
                Poi.UpdateEffectiveStyle();
                Poi.TriggerUpdated();
            });
        }

        private void CreateInitialPath()
        {
            removeLastPoint = false;
            editRouteNotification = new NotificationEventArgs
            {
                Id         = Guid.NewGuid(),
                Background = AppState.AccentBrush,
                Foreground = Brushes.White,
                Header     = "Edit route",
                Text       = "Click the route, including way points.",
                Duration   = TimeSpan.FromDays(1),
                Options    = new List<string> { "DONE" }
            };

            editRouteNotification.OptionClicked += (sender, args) =>
            {
                removeLastPoint = !args.UsesTouch; // Only remove the last point when the mouse was used.
                draw.CompleteDraw();
            };
            AppState.TriggerNotification(editRouteNotification);

            draw = new Draw(AppState.ViewDef.MapControl)
            {
                DrawMode = DrawMode.Polyline,
                LineSymbol = new LineSymbol
                {
                    Width = Poi.NEffectiveStyle.StrokeWidth.HasValue ? Poi.NEffectiveStyle.StrokeWidth.Value : 2,
                    Color = new SolidColorBrush(Poi.NEffectiveStyle.StrokeColor.HasValue ? Poi.NEffectiveStyle.StrokeColor.Value : Colors.Black)
                },
                IsEnabled = true,
            };
            // Add the first point (drop point)
            draw.AddVertex(webMercator.FromGeographic(new MapPoint(Poi.Position.Longitude, Poi.Position.Latitude)) as MapPoint);

            draw.DrawComplete += OnDrawingCompleted;
        }

        private void OnDrawingCompleted(object sender, DrawEventArgs e)
        {
            AppState.TriggerDeleteNotification(editRouteNotification);

            draw.IsEnabled = false;
            //Poi.Points.Add(start);
            var pl = e.Geometry as Polyline;
            if (pl == null) return;
            Poi.Points.Clear();
            foreach (var p in pl.Paths[0])
            {
                var convertedPoint = (MapPoint)webMercator.ToGeographic(new MapPoint(p.X, p.Y));
                Poi.Points.Add(new Point(convertedPoint.X, convertedPoint.Y));
            }
            if (removeLastPoint) Poi.Points.RemoveAt(Poi.Points.Count - 1);

            if (selectedPathType == PathType.GoogleDrivingDirections ||
                selectedPathType == PathType.GoogleWalkingDirections)
            {
                if (Poi.Points.Count < 2) RemoveRoute(null);
                start = Poi.Points.FirstOrDefault();
                Poi.Points.Remove(start);
                end = Poi.Points.LastOrDefault();
                Poi.Points.Remove(end);
                CreatePoiAndSubscribeToPositionChanged(start, TrackPoiType.Start);
                CreatePoiAndSubscribeToPositionChanged(end,   TrackPoiType.End);

                if (Poi.Points.Count > 0)
                {
                    var maxIndex = Math.Min(8, Poi.Points.Count);
                    for (var i = 0; i < maxIndex; i++)
                    {
                        var point = Poi.Points[i];
                        var id = CreatePoiAndSubscribeToPositionChanged(point, TrackPoiType.WayPoint);
                        WayPoints.Add(new WayPoint(point, id));
                    }
                }
                Poi.Points.Clear();
                DrawRoute();
            }
            else
                Execute.OnUIThread(() => Poi.TriggerUpdated());
        }

        private void RestoreState()
        {
            Poi.Deleted += (sender, e) => OnPoiDeleted(Poi, TrackPoiType.Route);

            foreach (var poi in RelatedPois)
            {
                FollowPoiPosition(poi);
                var trackPoiType = GetTrackPoiType(poi);
                var location = new Point(poi.Position.Longitude, poi.Position.Latitude);
                switch (trackPoiType)
                {
                    case TrackPoiType.Start:
                        start = location;
                        break;
                    case TrackPoiType.End:
                        end = location;
                        break;
                    case TrackPoiType.WayPoint:
                        WayPoints.Add(new WayPoint(location, poi.Id));
                        break;
                }
            }
        }

        private static AppStateSettings AppState { get { return AppStateSettings.Instance; } }

        private PathType SelectedPathType
        {
            get { return selectedPathType; }
            set
            {
                if (value == selectedPathType) return;
                selectedPathType = value;
                Poi.Labels[PathEditorPathTypeKey] = selectedPathType.ToString();
                CreatePath();
            }
        }

        /// <summary>
        /// Create a new path between start and end point.
        /// </summary>
        private void CreatePath()
        {
            ResetPath();

            switch (selectedPathType)
            {
                case PathType.StraightLine:
                    ClearWayPoints();
                    DrawStraightLine();
                    break;
                case PathType.Freehand:
                    ClearWayPoints();
                    DrawFreehand();
                    break;
                case PathType.MultiPoint:
                    ClearWayPoints();
                    DrawPointToPoint();
                    break;
                case PathType.GoogleDrivingDirections:
                case PathType.GoogleWalkingDirections:
                    DrawRoute();
                    break;
            }
        }

        private void ClearWayPoints()
        {
            var removeWayPoints = RelatedPois.Where(p => string.Equals(p.PoiTypeId, TrackPoiType.WayPoint.ToString(), StringComparison.CurrentCultureIgnoreCase));
            foreach (var poi in removeWayPoints) Model.Service.PoIs.Remove(poi);
            WayPoints.Clear();
        }

        private void ResetPath()
        {
            Poi.Labels[KeyLabelDistance] = Poi.Labels[KeyLabelTravelTime] = string.Empty;

            start = Poi.Points.FirstOrDefault();
            end   = Poi.Points.LastOrDefault();

            Poi.Points.Clear();

            Execute.OnUIThread(() => Poi.TriggerUpdated());
        }

        /// <summary>
        /// Draw a multi-point path.
        /// </summary>
        private void DrawPointToPoint()
        {
            AppState.TriggerNotification("Start drawing the multi-point path");

            draw = new Draw(AppState.ViewDef.MapControl)
            {
                DrawMode = DrawMode.Polyline,
                LineSymbol = new LineSymbol
                {
                    Width = 2,
                    //Color = Poi.NEffectiveStyle.StrokeColor
                },
                IsEnabled = true
            };
            draw.DrawComplete += OnDrawingCompleted;
        }

        /// <summary>
        /// Draw a freehand path.
        /// </summary>
        private void DrawFreehand()
        {
            AppState.TriggerNotification("Start drawing the path");

            draw = new Draw(AppState.ViewDef.MapControl)
            {
                DrawMode = DrawMode.Freehand,
                LineSymbol = new LineSymbol
                {
                    Width = 2,
                    //Color = SelectedColorBrush
                },
                IsEnabled = true
            };
            draw.DrawComplete += OnDrawingCompleted;
        }

        /// <summary>
        /// Draw a straight line between start and end.
        /// </summary>
        private void DrawStraightLine()
        {
            Poi.Points.Add(start);
            Poi.Points.Add(end);
            Execute.OnUIThread(() => Poi.TriggerUpdated());
        }

        #region Routing

        private BindableCollection<WayPoint> WayPoints { get; set; }

        /// <summary>
        /// Draw a route using Google
        /// </summary>
        /// <see cref="http://developers.google.com/maps/documentation/geocoding/?hl=nl"/>
        private async void DrawRoute()
        {
            await Task.Factory.StartNew(() =>
            {
                var mode = SelectedPathType == PathType.GoogleDrivingDirections
                    ? "driving"
                    : "walking";

                var wayPoints = string.Empty;
                if (WayPoints.Count > 0)
                {
                    while (WayPoints.Count > 8) WayPoints.RemoveAt(WayPoints.Count - 1);
                    wayPoints = string.Join("|", WayPoints.Where(wp => !wp.IsLastItem));
                }

                var gd = DirectionsUtils.GetDirections(
                    string.Format(CultureInfo.InvariantCulture, "{0},{1}", start.Y, start.X),
                    string.Format(CultureInfo.InvariantCulture, "{0},{1}", end.Y, end.X), mode, string.Format("optimize:true|{0}", wayPoints));
                if (gd == null) return;
                if (gd.Directions == null) return;
                var kmlPoints = DirectionsUtils.DecodeLatLong(gd.Directions.Polyline.points);
                foreach (var kmlPoint in kmlPoints)
                {
                    Poi.Points.Add(new Point(kmlPoint.Longitude, kmlPoint.Latitude));
                }

                int distanceInMeters;
                int.TryParse(gd.Directions.Distance.meters, NumberStyles.Integer, CultureInfo.InvariantCulture, out distanceInMeters);
                Poi.Labels[KeyLabelDistance] = Distance(distanceInMeters);

                int travelTimeInSeconds;
                int.TryParse(gd.Directions.Duration.seconds, NumberStyles.Integer, CultureInfo.InvariantCulture, out travelTimeInSeconds);
                var travelTime = TimeSpan.FromSeconds(travelTimeInSeconds);
                Poi.Labels[KeyLabelTravelTime] = travelTime.ToString("dd'.'hh':'mm':'ss");

                ShowRouteInformation(distanceInMeters, travelTime);

                Execute.OnUIThread(() =>
                {
                    Poi.TriggerLabelChanged(KeyLabelDistance, string.Empty, string.Empty);
                    Poi.TriggerUpdated();
                });
            });
        }

        private void ShowRouteInformation(int distanceInMeters, TimeSpan travelTime)
        {
            AppState.TriggerDeleteNotification(notification);
            var mode = string.Empty;
            switch (selectedPathType)
            {
                case PathType.GoogleDrivingDirections:
                    mode = "driving";
                    break;
                case PathType.GoogleWalkingDirections:
                    mode = "walking";
                    break;
            }
            var distance = Distance(distanceInMeters); 

            notification = new NotificationEventArgs
            {
                Id         = Guid.NewGuid(),
                Background = AppState.AccentBrush,
                Foreground = Brushes.White,
                Header     = "Route info",
                Text       = string.Format("The {0} distance is {1} and takes {2}.", mode, distance, travelTime.Humanize(2)),
                Duration   = TimeSpan.FromDays(1),
            };

            AppState.TriggerNotification(notification);
        }

        private static string Distance(int distanceInMeters)
        {
            return distanceInMeters < 1000
                ? string.Format("{0:0,0} m", distanceInMeters)
                : string.Format("{0:0.0} km", distanceInMeters/1000);
        }

        private void ServiceTapped(object sender, TappedEventArgs e)
        {
            if (WayPoints.Count >= 8) return; // Max waypoints in Google
            var poi = e.Content as PoI;
            if (poi == null || poi != Poi) return;
            AddWayPoint(e.TapPoint);
        }

        private void AddWayPoint(Point tapPoint)
        {
            lastTapPoint = tapPoint;
            if (addingWayPoint) return;
            addingWayPoint = true;

            var notificationEventArgs = new NotificationEventArgs
            {
                Id         = Guid.NewGuid(),
                Background = AppState.AccentBrush,
                Foreground = Brushes.White,
                Header     = "Add way point",
                Text       = "At the current location?",
                Duration   = TimeSpan.FromSeconds(5),
                Options    = new List<string> { "YES", "NO" }
            };

            notificationEventArgs.Closing += (sender, args) => addingWayPoint = false;
            notificationEventArgs.OptionClicked += (sender, args) =>
            {
                addingWayPoint = false;
                switch (args.Option)
                {
                    case "NO":
                        return;
                    case "YES":
                        CreatePoiAndSubscribeToPositionChanged(lastTapPoint, TrackPoiType.WayPoint);
                        return;
                }
            };
            AppState.TriggerNotification(notificationEventArgs);
        }

        private Guid CreatePoiAndSubscribeToPositionChanged(Point pos, TrackPoiType trackPoiType)
        {
            var poi = CreatePoI(pos, trackPoiType);
            FollowPoiPosition(poi);
            return poi.Id;
        }

        private void FollowPoiPosition(BaseContent poi)
        {
            var trackPoiType = GetTrackPoiType(poi);

            poi.Deleted += (sender, e) => OnPoiDeleted(poi, trackPoiType);

            var posChanged = Observable.FromEventPattern<PositionEventArgs>(ev => poi.PositionChanged += ev, ev => poi.PositionChanged -= ev);
            posChanged.Throttle(TimeSpan.FromMilliseconds(250)).Subscribe(k =>
            {
                ResetPath();
                var newPoint = new Point(poi.Position.Longitude, poi.Position.Latitude);
                switch (trackPoiType)
                {
                    case TrackPoiType.Start:
                        start = newPoint;
                        break;
                    case TrackPoiType.End:
                        end = newPoint;
                        break;
                    case TrackPoiType.WayPoint:
                        var wayPoint = WayPoints.FirstOrDefault(w => w.Id == poi.Id);
                        if (wayPoint == null)
                        {
                            wayPoint = new WayPoint(newPoint, poi.Id);
                            WayPoints.Add(wayPoint);
                        }
                        else
                            wayPoint.Point = newPoint;
                        break;
                }
                DrawRoute();
            });
        }

        private bool isDeleting;
        private NotificationEventArgs editRouteNotification;

        private async void OnPoiDeleted(BaseContent poi, TrackPoiType trackPoiType)
        {
            if (isDeleting) return;
            AppState.TriggerDeleteNotification(notification);

            switch (trackPoiType)
            {
                default:
                    isDeleting = true;
                    await RemoveRoute(poi);
                    isDeleting = false;
                    break;
                case TrackPoiType.WayPoint:
                    var wayPoint = WayPoints.FirstOrDefault(w => w.Id == poi.Id);
                    if (wayPoint != null) WayPoints.Remove(wayPoint);
                    ResetPath();
                    DrawRoute();
                    break;
            }
        }

        private Task RemoveRoute(BaseContent poi)
        {
            return new TaskFactory().StartNew(() =>
            {
                var remove = RelatedPois.ToList();
                remove.Add(Poi);
                remove.Remove(poi);
                Execute.OnUIThread(() =>
                {
                    try
                    {
                        foreach (var p in remove) Model.Service.PoIs.Remove(p);
                    }
                    catch (SystemException e)
                    {
                        Logger.Log("PathEditorPoi", "Error deleting PoI", e.Message, Logger.Level.Error, true);
                    }
                });
            });
        }

        private static TrackPoiType GetTrackPoiType(BaseContent poi)
        {
            TrackPoiType trackPoiType;
            Enum.TryParse(poi.ContentId, out trackPoiType);
            return trackPoiType;
        }

        private PoI CreatePoI(Point pos, TrackPoiType trackPoiType)
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
                Style     = new PoIStyle
                {
                    DrawingMode       = DrawingModes.Point,
                    FillColor         = Colors.Gold,
                    StrokeColor       = Colors.Black,
                    CallOutFillColor  = Colors.White,
                    CallOutForeground = Colors.Black,
                    StrokeOpacity     = 1,
                    StrokeWidth       = 2,
                    InnerTextLabel    = "InnerText",
                    TitleMode         = TitleModes.Bottom,
                    TapMode           = TapMode.CallOutPopup,
                    InnerTextColor    = Colors.Black, 
                    CanEdit           = false
                },
                Position              = new Position(pos.X, pos.Y),
                MetaInfo              = new List<MetaInfo>
                {
                    new MetaInfo { IsEditable = false, Label = "Name", Title = "Name", Type = MetaTypes.text, VisibleInCallOut = true }
                }
            };

            switch (trackPoiType)
            {
                case TrackPoiType.Start:
                    poi.Labels["InnerText"] = "α";
                    break;
                case TrackPoiType.End:
                    poi.Labels["InnerText"] = "Ω";
                    break;
                case TrackPoiType.WayPoint:
                    poi.Labels["InnerText"] = "*";
                    break;
            }

            Execute.OnUIThread(() =>
            {
                poi.TriggerUpdated();
                Model.Service.PoIs.Add(poi);
            });

            return poi;
        }

        private IEnumerable<BaseContent> RelatedPois
        {
            get
            {
                var id = Poi.Id.ToString();
                return Model.Service.PoIs.Where(p => string.Equals(p.UserId, id)).ToList();
            }
        }

        #endregion Routing
    }
}