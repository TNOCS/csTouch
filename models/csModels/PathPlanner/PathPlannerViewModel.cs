using System.Threading.Tasks;
using Caliburn.Micro;
using csShared;
using csShared.Controls.Popups.MapCallOut;
using csShared.Controls.Popups.MenuPopup;
using csShared.Utils;
using DataServer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;
using Task = System.Threading.Tasks.Task;

namespace csModels.PathPlanner
{
    // TODO Throttle POI updates
    // TODO Give every person to be tracked its own layer, so we can hide them selectively.
    // TODO Re-enact De Bruin scenario.
    // TODO Use routing algorithm to compute the route between two points.

    [Export(typeof(IScreen))]
    public class PathPlannerViewModel : Screen, IEditableScreen
    {
        private const string KeyLabelStartTime = "StartTime";
        private const string KeyLabelEndTime   = "EndTime";
        private const string KeyLabelDuration  = "Duration";
        private const string KeyLabelDistance  = "Distance";
        private const string KeyLabelPathIndex = "PathIndex";

        private const string LabelCreatorId        = ".CreatorId";
        private const string LabelSourceId         = ".SourceId";
        private const string LabelSinkId           = ".SinkId";
        private const string LabelStrokeColor      = ".StrokeColor";
        private const string LabelVisitedLocations = ".VisitedLocations"; // NOTE the key with the same name in PathPlannerModel

        private const string AnimationModel       = "AnimationModel";
        private const string Animations           = "Animations";
        private const string ParameterLinkPoiType = "PathPoiType";
        private const string KeyLatitude          = "[Lat]";
        private const string KeyLongitude         = "[Lon]";
        private const string KeyOrientationSensor = "[Orient]";
        private const string KeyAnimateMove       = "Move";
        private const string IsPathActive         = "IsActive";
        private const string DefaultLinkPoiType   = "PathPlannerLink";

        private static readonly AppStateSettings AppState = AppStateSettings.Instance;
        private readonly List<string> animations = new List<string>();
        private VisitedLocations visitedLocations = new VisitedLocations();
        private VisitedLocation selectedVisitedLocation;
        private string visitedLocationName;
        private List<ModelParameter> modelParameters;
        private bool canEdit;
        private string linkPoiType;

        private string KeyCreatorId { get { return Model.Id + LabelCreatorId; } }

        private string KeySourceId { get { return Model.Id + LabelSourceId; } }

        private string KeySinkId { get { return Model.Id + LabelSinkId; } }

        private string KeyStrokeColor { get { return Model.Id + LabelStrokeColor; } }

        private string KeyVisitedLocations { get { return Model.Id + LabelVisitedLocations; } }

        /// <summary>
        /// Initialize the view model.
        /// </summary>
        public void Initialize()
        {
            modelParameters = Model.Model.Parameters;
            var animationModel = modelParameters.FirstOrDefault(p => string.Equals(p.Name, AnimationModel, StringComparison.InvariantCultureIgnoreCase));
            if (animationModel != null) PoI.Labels["TimeBased.Model"] = animationModel.Value;
            var anima = modelParameters.FirstOrDefault(p => string.Equals(p.Name, Animations, StringComparison.InvariantCultureIgnoreCase));
            if (anima != null)
            {
                foreach (var animation in anima.Value.Split(new[] { ';', ',', '|' }, StringSplitOptions.RemoveEmptyEntries)) animations.Add(animation);
                foreach (var vl in VisitedLocations) vl.Animations = animations;
            }

            Model.Service.HasSensorData = true;

            AppState.TimelineManager.PropertyChanged    += TimelineManagerOnPropertyChanged;
            //AppState.TimelineManager.TimeContentChanged += (sender, args) => SetActivityOfPath();

            PoI.Deleted += (sender, e) => OnPoiDeleted();

            linkPoiType = RetreiveLinkPoiType();
            if (!string.IsNullOrEmpty(linkPoiType)) return;
            linkPoiType = DefaultLinkPoiType;
            Model.Service.PoITypes.Add(new PoI
            {
                Name            = linkPoiType,
                ContentId       = linkPoiType,
                IsVisibleInMenu = false,
                Layer           = Model.Id + "Path",
                Service         = Model.Service,
                Style           = new PoIStyle
                {
                    DrawingMode = DrawingModes.Polyline,
                    TapMode     = TapMode.None,
                    StrokeColor = Colors.Blue,
                    StrokeWidth = 4,
                    CanEdit     = false
                }
            });
            //UpdateVisitedPath();
        }

        private async void TimelineManagerOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!string.Equals(e.PropertyName, "IsPlaying")) return;
            NotifyOfPropertyChange(() => CanSelectTransition);
            NotifyOfPropertyChange(() => CanAddLocation);
            NotifyOfPropertyChange(() => CanRemoveVisitedLocation);
            if (!AppState.TimelineManager.IsPlaying) return;
            await ConvertPathToSensorData();
            // throttle position changed events
            var posChanged = Observable.FromEventPattern<PositionEventArgs>(ev => PoI.PositionChanged += ev, ev => PoI.PositionChanged -= ev);
            posChanged.TakeWhile(k => AppState.TimelineManager.IsPlaying).Throttle(TimeSpan.FromMilliseconds(250)).Subscribe(k => PoIOnPositionChanged());
        }

        private void PoIOnPositionChanged()
        {
            PoI.Labels["TimeBased.Lat"]     = PoI.Position.Latitude .ToString("##.######", CultureInfo.InvariantCulture);
            PoI.Labels["TimeBased.Lon"]     = PoI.Position.Longitude.ToString("##.######", CultureInfo.InvariantCulture);
            PoI.Labels["TimeBased.Orient"]  = PoI.GetSensorValue(KeyOrientationSensor).ToString("##.######", CultureInfo.InvariantCulture);
            if (animations.Count > 0)
            {
                PoI.Labels["TimeBased.Move"] = PoI.Sensors.ContainsKey(KeyAnimateMove)
                    ? animations[(int) PoI.Sensors[KeyAnimateMove].FocusValue]
                    : animations[0];
            }
            PoI.OnLabelChanged("TimeBased");
            if (AppState.TimelineManager.IsPlaying) return;
            Execute.OnUIThread(() => PoI.Updated = AppState.TimelineManager.CurrentTime);
        }

        /// <summary>
        /// Set the activity label of the PoI and the path, based on whether the current time is 
        /// still within the defined time (visits).
        /// </summary>
        private void SetActivityOfPath()
        {
            //ThreadPool.QueueUserWorkItem(delegate
            //{
            //    if (PoI.Sensors.ContainsKey("[Lat]") && PoI.Sensors.ContainsKey("[Lon]"))
            //    {
            //        Execute.OnUIThread(() =>
            //        {
            //            PoI.Position.Latitude  = PoI.Sensors["[Lat]"].FocusValue;
            //            PoI.Position.Longitude = PoI.Sensors["[Lon]"].FocusValue;
            //            PoI.TriggerPositionChanged();
            //        });
            //    }
            //});
            
            
            var currentTime = AppState.TimelineManager.FocusTime;
            var isActive = visitedLocations.IsActive(currentTime) ? "1" : "0";

            if (isActive == "1")
            {
                var a = 10;
            }

            if (!PoI.Labels.ContainsKey(IsPathActive) || PoI.Labels[IsPathActive] != isActive)
            {
                PoI.Labels[IsPathActive] = isActive;
                Execute.OnUIThread(() => PoI.TriggerLabelChanged(IsPathActive, string.Empty, string.Empty));
            }
            foreach (var link in Links)
            {
                if (link.Labels.ContainsKey(IsPathActive) && link.Labels[IsPathActive] == isActive) continue;
                link.Labels[IsPathActive] = isActive;
                var link1 = link;
                Execute.OnUIThread(() => link1.TriggerLabelChanged(IsPathActive, string.Empty, string.Empty));
                //link.TriggerLabelChanged(IsActive, string.Empty, string.Empty);
            }
        }

        public IModel Model { private get; set; }

        private VisitedLocation SelectedVisitedLocation
        {
            get { return selectedVisitedLocation; }
            set { selectedVisitedLocation = value; NotifyOfPropertyChange(() => SelectedVisitedLocation); }
        }

        public VisitedLocations VisitedLocations
        {
            private get { return visitedLocations; }
            set { visitedLocations = value; NotifyOfPropertyChange(() => VisitedLocations); }
        }

        public string VisitedLocationName
        {
            get { return visitedLocationName; }
            set { visitedLocationName = value; NotifyOfPropertyChange(() => VisitedLocationName); }
        }

        #region Color selection

        private string selectedColor = "LightGray";

        public string SelectedColor
        {
            get { return selectedColor; }
            set
            {
                selectedColor = value;
                NotifyOfPropertyChange(() => SelectedColor);
                NotifyOfPropertyChange(() => SelectedColorBrush);
            }
        }

        public SolidColorBrush SelectedColorBrush { get { return new BrushConverter().ConvertFromString(SelectedColor) as SolidColorBrush; } }

        private static MenuPopupViewModel GetMenu(FrameworkElement fe)
        {
            var menu = new MenuPopupViewModel
            {
                RelativeElement = fe,
                RelativePosition = new Point(10, 30),
                TimeOut = new TimeSpan(0, 0, 0, 15),
                VerticalAlignment = VerticalAlignment.Bottom,
                DisplayProperty = string.Empty,
                AutoClose = true
            };
            return menu;
        }

        public void SelectColor(FrameworkElement el)
        {
            var m = GetMenu(el);
            m.AddMenuItems(new[] { "Gray", "Yellow", "Black", "Green", "Blue", "Orange", "Purple", "Brown", "Red" });
            m.Selected += (s, f) =>
            {
                SelectedColor = f.Object.ToString();
            };
            AppState.Popups.Add(m);
        }

        #endregion Color selection

        public static bool CanAddLocation { get { return !AppState.TimelineManager.IsPlaying; } }

        public void AddLocation()
        {
            var title = string.IsNullOrEmpty(visitedLocationName) ? Model.Id : visitedLocationName;
            // Only add the visitedLocation in case its unique
            var timeOfVisit = AppState.TimelineManager.FocusTime;
            var visitedLocation = VisitedLocations.FirstOrDefault(n => string.Equals(n.Title, title, StringComparison.InvariantCultureIgnoreCase))
                ?? new VisitedLocation { Title = title, TimeOfVisit = timeOfVisit, Position = PoI.Position, StrokeColor = SelectedColor, Animations = animations };
            if (!VisitedLocations.Contains(visitedLocation))
            {
                var i = 0;
                for (; i < visitedLocations.Count; i++)
                {
                    var curLoc = visitedLocations[i];
                    if ((Math.Abs(curLoc.TimeOfVisit.Ticks - timeOfVisit.Ticks) < TimeSpan.FromSeconds(1).Ticks))
                    {
                        AppState.TriggerNotification("Warning", "Please change the focus time before adding a point.");
                        return;
                    }
                    if (curLoc.TimeOfVisit > timeOfVisit) break;
                }
                VisitedLocations.Insert(i, visitedLocation);
                SaveVisitedLocationsLabel();
                UpdateVisitedPath();
            }
            VisitedLocationName = string.Empty;
        }

        /// <summary>
        /// Update all paths when a visited location has been added or removed.
        /// Try to limit the update only to the paths that have actually changed.
        /// </summary>
        public async void UpdateVisitedPath()
        {
            var existingLinks = Model.Service.PoIs.OfType<PoI>().Where(p => p.Labels != null
                                                                            && p.Labels.ContainsKey(KeyCreatorId)
                                                                            && string.Equals(p.Labels[KeyCreatorId], PoI.Id.ToString(), StringComparison.InvariantCultureIgnoreCase)).ToList();
            for (var i = 0; i < VisitedLocations.Count - 1; i++)
            {
                var visitedSource = VisitedLocations[i];
                var visitedSink = VisitedLocations[i + 1];
                var existingLink = existingLinks.FirstOrDefault(p =>
                    p.Labels.ContainsKey(KeySourceId)
                    && string.Equals(p.Labels[KeySourceId], visitedSource.Id.ToString(), StringComparison.InvariantCultureIgnoreCase)
                    && p.Labels.ContainsKey(KeySinkId)
                    && string.Equals(p.Labels[KeySinkId], visitedSink.Id.ToString(), StringComparison.InvariantCultureIgnoreCase));
                if (existingLink != null)
                {
                    existingLink.Name = string.Format("Step {0}", i + 1);
                    existingLink.Labels[KeyLabelPathIndex] = i.ToString("00000");
                    existingLinks.Remove(existingLink);
                }
                else
                {
                    var newLink = CreateLinkBetweenPoIs(visitedSource, visitedSink);
                    newLink.Name = string.Format("Step {0}", i + 1);
                    newLink.Labels[KeyLabelPathIndex] = i.ToString("00000");
                    Execute.OnUIThread(() => Model.Service.PoIs.Add(newLink));
                }
            }
            foreach (var link in existingLinks) Model.Service.PoIs.Remove(link);
            await ConvertPathToSensorData();
        }

        /// <summary>
        /// All PoIs that are created, representing the path.
        /// </summary>
        private IEnumerable<PoI> Links
        {
            get
            {
                return Model.Service.PoIs.OfType<PoI>()
                    .Where(p => p.Labels != null
                                && p.Labels.ContainsKey(KeyCreatorId)
                                && string.Equals(p.Labels[KeyCreatorId], PoI.Id.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    .OrderBy(p => p.Labels[KeyLabelPathIndex])
                    .ToList();
            }
        }

        /// <summary>
        /// Convert the path to sensor data.
        /// </summary>
        /// <returns></returns>
        private Task ConvertPathToSensorData()
        {
            return Task.Run(() =>
            {
                // Initialize poi sensors
                if (PoI.Sensors == null) PoI.Sensors        = new SensorSet();
                csEvents.Sensors.DataSet dataSet;
                PoI.Sensors.TryRemove(KeyLongitude,         out dataSet);
                PoI.Sensors.TryRemove(KeyLatitude,          out dataSet);
                PoI.Sensors.TryRemove(KeyAnimateMove,       out dataSet);
                PoI.Sensors.TryRemove(KeyOrientationSensor, out dataSet);

                foreach (var link in Links)
                {
                    DateTime startTime, endTime;
                    if (!DateTime.TryParse(link.Labels[KeyLabelStartTime], CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out startTime)) return;
                    if (!DateTime.TryParse(link.Labels[KeyLabelEndTime  ], CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out endTime)) return;

                    var totalLength = 0d;
                    var numberOfPoints = link.Points.Count - 1;
                    if (numberOfPoints < 1) continue;
                    var segmentLengths = new double[numberOfPoints];

                    for (var i = 0; i < numberOfPoints; i++)
                    {
                        var p1 = link.Points[i];
                        var p2 = link.Points[i + 1];
                        segmentLengths[i] = ComputeLengthOfSegment(p1, p2);
                        totalLength += segmentLengths[i];
                    }

                    link.Labels[KeyLabelDistance] = totalLength.ToString(CultureInfo.InvariantCulture);

                    var curTime = startTime;
                    // Interpolate linearly along the path between p1 and p2
                    if (totalLength>0)
                    for (var i = 0; i < numberOfPoints; i++)
                    {
                        var p1 = link.Points[i];
                        var p2 = link.Points[i + 1];

                        var lengthOfCurrentSegment   = segmentLengths[i];
                        var durationOfCurrentSegment = TimeSpan.FromTicks((long)((endTime - startTime).Ticks * lengthOfCurrentSegment / totalLength));
                        var endTimeOfCurrentSegment  = curTime + durationOfCurrentSegment;

                        var heading = new Vector(p2.X - p1.X, p2.Y - p1.Y) / durationOfCurrentSegment.TotalSeconds;
                        var currentPoint = p1;
                        PoI.SetSensorValue(curTime, KeyOrientationSensor, heading.ToAngleInDegrees());
                        PoI.SetSensorValue(curTime, KeyAnimateMove, link.Labels.ContainsKey(KeyAnimateMove)
                            ? animations.IndexOf(link.Labels[KeyAnimateMove])
                            : 0);
                        for (var t = curTime; t < endTimeOfCurrentSegment; t += TimeSpan.FromSeconds(1))
                        {
                            PoI.SetSensorValue(t, KeyLongitude, currentPoint.X);
                            PoI.SetSensorValue(t, KeyLatitude, currentPoint.Y);
                            currentPoint += heading; // NOTE: Assumes a stepsize of 1 second
                        }
                        // Reset counters
                        curTime = endTimeOfCurrentSegment;
                    }
                }
                //Model.Service.HasSensorData = PoI.Sensors.ContainsKey(KeyLongitude);
            });
        }

        private static double ComputeLengthOfSegment(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        public void SaveVisitedLocationsLabel()
        {
            PoI.Labels[KeyVisitedLocations] = string.Join(";", VisitedLocations.Select(n => n.ToString()).ToArray());
            Model.Service.SaveXml();
        }

        /// <summary>
        /// Retreive the PoiTypeId of the link.
        /// </summary>
        /// <returns></returns>
        private string RetreiveLinkPoiType()
        {
            var linkPoiTypeParameter = modelParameters.FirstOrDefault(p => string.Equals(p.Name, ParameterLinkPoiType, StringComparison.InvariantCultureIgnoreCase));
            if (linkPoiTypeParameter == null) return string.Empty;
            var poiType = linkPoiTypeParameter.Value;
            return (Model.Service.PoITypes.OfType<PoI>()
                .Any(pt => string.Equals(pt.PoiId, poiType, StringComparison.InvariantCultureIgnoreCase)))
                ? poiType
                : Model.Service.PoITypes.OfType<PoI>().Any(pt => string.Equals(pt.PoiId, DefaultLinkPoiType, StringComparison.InvariantCultureIgnoreCase))
                    ? DefaultLinkPoiType
                    : string.Empty;
        }

        /// <summary>
        /// Create a link between source and sink.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="sink"></param>
        /// <returns></returns>
        private PoI CreateLinkBetweenPoIs(VisitedLocation source, VisitedLocation sink)
        {
            var link = new PoI
            {
                Id        = Guid.NewGuid(),
                Name      = "Path",
                Position  = source.Position, // Although not needed, if I don't supply this, the app freezes
                PoiTypeId = linkPoiType,
                UserId    = PoI.Id.ToString(),
                Layer     = PoI.Layer + "_path",

                Labels = new Dictionary<string, string> {
                    {KeyCreatorId,      PoI.Id.ToString()},
                    {KeySourceId,       source.Id.ToString()},
                    {KeySinkId,         sink.Id.ToString()},
                    {KeyStrokeColor,    sink.StrokeColor},
                    {KeyLabelStartTime, source.TimeOfVisit.ToString("yyyy-MM-dd HH:mm:ss")},
                    {KeyLabelEndTime,   sink  .TimeOfVisit.ToString("yyyy-MM-dd HH:mm:ss")},
                    {KeyLabelDuration,  (sink.TimeOfVisit - source.TimeOfVisit).ToString("dd'.'hh':'mm':'ss")},
                    {KeyAnimateMove,    source.Transition},
                    {"IsActive",        "false"}
                },
                Points = CreatePath(source, sink)
            };
            var convertFromString = ColorConverter.ConvertFromString(sink.StrokeColor);
            link.Style = new PoIStyle { CanMove = false, CanDelete = false, StrokeColor = convertFromString != null ? (Color)convertFromString : Colors.Blue, StrokeWidth = 3 };
            link.Style.DrawingMode = DrawingModes.Polyline;
            link.UpdateEffectiveStyle();
            var s = link.DrawingMode;
            return link;
        }

        private IEnumerable<BaseContent> RelatedPois
        {
            get
            {
                var id = PoI.Id.ToString();
                return Model.Service.PoIs.Where(p => string.Equals(p.UserId, id)).ToList();
            }
        }

        private async void OnPoiDeleted()
        {
            await RemoveRoute();
        }

        private Task RemoveRoute()
        {
            return new TaskFactory().StartNew(() =>
            {
                var remove = RelatedPois.ToList();
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

        /// <summary>
        /// Create a path between source and sink.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="sink"></param>
        /// <returns>A path containing two or more points.</returns>
        private static ObservableCollection<Point> CreatePath(VisitedLocation source, VisitedLocation sink)
        {
            return new ObservableCollection<Point> {
                new Point {X = source.Position.Longitude, Y = source.Position.Latitude},
                new Point {X = sink.Position.Longitude, Y = sink.Position.Latitude}
            };
        }

        public bool CanSelectTransition { get { return !AppState.TimelineManager.IsPlaying; } }

        public void SelectTransition(VisitedLocation visitedLocation, FrameworkElement el)
        {
            if (animations == null) return;
            SelectedVisitedLocation = visitedLocation;
            var m = GetMenu(el);
            foreach (var animation in animations)
                m.AddMenuItem(animation.ToSentenceCase());

            //foreach (MovementAnimationMode mode in Enum.GetValues(typeof(MovementAnimationMode)))
            //{
            //    m.AddMenuItem(mode.ToString().ToSentenceCase());
            //}
            m.Selected += (s, f) =>
            {
                var selectedMode = f.Object.ToString();
                foreach (var mode in animations.Where(mode => string.Equals(selectedMode, mode.ToSentenceCase())))
                {
                    SelectedVisitedLocation.Transition = mode;
                    foreach (var link in Model.Service.PoIs.OfType<PoI>().Where(p =>
                        p.Labels != null &&
                        p.Labels.ContainsKey(KeyCreatorId) &&
                        p.Labels.ContainsKey(KeySourceId) &&
                        string.Equals(p.Labels[KeyCreatorId], PoI.Id.ToString(), StringComparison.InvariantCultureIgnoreCase) &&
                        string.Equals(p.Labels[KeySourceId], SelectedVisitedLocation.Id.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        .OrderBy(p => p.Labels[KeyLabelPathIndex]))
                    {
                        link.Labels[KeyAnimateMove] = mode;
                        //if (!AppState.TimelineManager.IsPlaying || !PoI.Sensors.ContainsKey(KeyAnimateMove)) continue;
                        //PoI.Sensors[KeyAnimateMove].Data.Keys.AsParallel().ForEach(k => PoI.Sensors[KeyAnimateMove].Data[k] = animation);                              
                    }
                }
                SaveVisitedLocationsLabel();
            };
            AppStateSettings.Instance.Popups.Add(m);
        }

        public bool CanRemoveVisitedLocation { get { return !AppState.TimelineManager.IsPlaying; } }

        public void RemoveVisitedLocation(VisitedLocation visitedLocation)
        {
            var notificationEventArgs = new NotificationEventArgs
            {
                Id         = Guid.NewGuid(),
                Background = Brushes.Red,
                Foreground = Brushes.Black,
                Header     = "Delete location?",
                Text       = string.Format("Do you want to delete {0}?", visitedLocation.Title),
                Duration   = TimeSpan.FromDays(1),
                Options    = new List<string> { "YES", "NO" }
            };

            notificationEventArgs.OptionClicked += (sender, args) =>
            {
                switch (args.Option)
                {
                    case "NO":
                        return;
                    case "YES":
                        VisitedLocations.Remove(visitedLocation);
                        SaveVisitedLocationsLabel();
                        UpdateVisitedPath();
                        break;
                }
            };
            AppState.TriggerNotification(notificationEventArgs);
        }

        public void JumpToTime(VisitedLocation visitedLocation)
        {
            AppState.TimelineManager.SetFocusTime(visitedLocation.TimeOfVisit);
            //AppState.TimelineManager.ForceTimeChanged();
        }

        public bool CanEdit
        {
            get { return canEdit; }
            set
            {
                if (canEdit == value) return;
                canEdit = value;
                NotifyOfPropertyChange(() => CanEdit);
            }
        }

        public MapCallOutViewModel CallOut { get; set; }

        public PoI PoI { private get; set; }
    }
}
