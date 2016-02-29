using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Xml.Serialization;
using Caliburn.Micro;
using DataServer;
using Dis2PoiService.DisMessages;
using csShared;
using csShared.Interfaces;
using DetonationType = Dis2PoiService.DisMessages.DetonationType;
using EntityType = Dis2PoiService.DisMessages.EntityType;
using FireType = Dis2PoiService.DisMessages.FireType;
using PointOfImpactType = Dis2PoiService.DisMessages.PointOfImpactType;
using PointOfOriginType = Dis2PoiService.DisMessages.PointOfOriginType;
using PositionType = Dis2PoiService.DisMessages.PositionType;
using TrackType = Dis2PoiService.DisMessages.TrackType;

namespace Dis2PoiService {
    [Export(typeof(IPlugin))]
    public class Dis2PoiServicePlugin : PropertyChangedBase, IPlugin 
    {
        private const string EntityId = "id";
        private const string DsdServiceName = "DIS";

        private bool isRunning;
        private bool hideFromSettings;
        private ISettingsScreen settings;
        private DataServerBase dataServer;
        private PoiService ps;
        private readonly Dictionary<string, PoI> poiMap = new Dictionary<string, PoI>();
        
        public string Name {
            get { return "Dis2PoiServicePlugin"; }
        }
        
        public AppStateSettings AppState { get; set; }
        
        public bool IsRunning {
            get { return isRunning; }
            set { isRunning = value; NotifyOfPropertyChange(() => IsRunning); }
        }
        
        public string Icon {
            get { return @"icons\bookmarks.png"; }
        }
        
        public int Priority {
            get { return 3; }
        }
        
        public IPluginScreen Screen { get; set; }

        public bool HideFromSettings {
            get { return hideFromSettings; }
            set { hideFromSettings = value; NotifyOfPropertyChange(() => HideFromSettings); }
        }

        public bool CanStop { get { return true; } }

        public ISettingsScreen Settings {
            get { return settings; }
            set { settings = value; NotifyOfPropertyChange(() => Settings); }
        }

        public DataServerBase DataServer {
            get { return dataServer; }
            set {
                if (dataServer == value) return;
                dataServer = value;
                InitializeDis2PoiService();
                NotifyOfPropertyChange(() => DataServer);
            }
        }

        private void InitializeDis2PoiService() {
            if (dataServer == null) return;

            foreach (var template in dataServer.Templates.Where(service => service.Name.Contains(DsdServiceName))) {
                ps = template;
                StartServiceFromTemplate();
                break;
            }
            if (ps == null) return;

            var client = AppState.Imb;
            client.Subscribe("Entity.Message",     new XmlSerializer(typeof(DisMessages.Message)), OnEntityMessage);
            client.Subscribe("Fire.Message",       new XmlSerializer(typeof(DisMessages.Message)), OnDisMessage);
            client.Subscribe("Detonation.Message", new XmlSerializer(typeof(DisMessages.Message)), OnDisMessage);
            client.Subscribe("Impact.Message",     new XmlSerializer(typeof(DisMessages.Message)), OnDisMessage);
            client.Subscribe("Origin.Message",     new XmlSerializer(typeof(DisMessages.Message)), OnDisMessage);
            client.Subscribe("Track.Message",      new XmlSerializer(typeof(DisMessages.Message)), OnTrackMessage);
        }

        private async void StartServiceFromTemplate() {
            var templateFile = Path.Combine(ps.Folder, DsdServiceName + ".dsd");
            var refDataService = await PoiService.CreateTemplateBasedService(ps.Folder, templateFile);
            
            if (refDataService == null) return;
            refDataService.Initialized += (sender, args) => {
                ps = refDataService;
                ps.MakeOnline();
            };
        }

        public void Init() { }

        public void Start() {
            IsRunning = true;
        }

        public void Pause() {
            IsRunning = false;
        }

        public void Stop() {
            IsRunning = false;
            dataServer.UnSubscribe(ps);
            try
            {
                File.Delete(ps.FileName);
            }
            catch (Exception e)
            {
                AppState.TriggerNotification(e.Message);
            }
        }

        #region DIS message processing

        private void OnEntityMessage(string theChannelName, object message) {
            var disMessage = message as DisMessages.Message;
            if (disMessage == null) return;
            var entity = disMessage.Entity;
            var name = entity.Name;
            if (string.IsNullOrEmpty(name)) return;
            var pois = ps.PoIs;
            var existingPoi = pois.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.InvariantCultureIgnoreCase)) as PoI;
            if (existingPoi != null) {
                if (entity.DamageState.Equals("Destroyed") || entity.ActiveSpecified && !entity.Active)
                    pois.Remove(existingPoi);
                else
                    SetPoiLocation(entity.Position, entity.Course, entity.Time, existingPoi);
            }
            else {
                Execute.OnUIThread(() => {
                    var poi = ConvertEntityToPoI(disMessage.Entity);
                    if (poi != null) pois.Add(poi);
                });
            }
        }

        private void OnTrackMessage(string theChannelName, object message) {
            var trackMessage = message as DisMessages.Message;
            if (trackMessage == null) return;
            var track = trackMessage.Track;
            if (track == null) return;
            var pois = ps.PoIs;
            var existingPoi = pois.FirstOrDefault(p => p.Name.Equals(track.Name)) as PoI;
            if (existingPoi != null)
                SetPoiLocation(track.Position, new CourseType(), track.Time, existingPoi);
            else {
                var poi = ConvertTrackToPoI(track);
                if (poi != null) pois.Add(poi);
            }
        }

        private PoI ConvertTrackToPoI(TrackType track) {
            var poi = CreatePoiFromClassification("Track");
            if (poi == null) return null;
            poi.Name = track.Name;
            SetPoiLocation(track.Position, new CourseType(), track.Time, poi);
            return poi;
        }

        private void OnDisMessage(string theChannelName, object message) {
            var disMessage = message as DisMessages.Message;
            if (disMessage == null) return;
            PoI poi = null;
            if (disMessage.Entity != null)
                poi = ConvertEntityToPoI(disMessage.Entity);
            else if (disMessage.Detonation != null)
                poi = ConvertDetonationToPoI(disMessage.Detonation);
            else if (disMessage.Fire != null)
                poi = ConvertFireToPoI(disMessage.Fire);
            else if (disMessage.PointOfImpact != null)
                poi = ConvertPointOfImpactToPoI(disMessage.PointOfImpact);
            else if (disMessage.PointOfOrigin != null)
                poi = ConvertPointOfOriginToPoI(disMessage.PointOfOrigin);
            var pois = ps.PoIs;
            if (poi != null) pois.Add(poi);
        }

        private PoI ConvertPointOfOriginToPoI(PointOfOriginType pointOfOrigin) {
            var pois = ps.PoIs;
            var poi = pois.FirstOrDefault(p => p.Name.Equals(pointOfOrigin.Name)) as PoI;
            if (poi == null) {
                poi = CreatePoiFromClassification("PointOfOrigin");
                if (poi == null) return null;
                poi.Name = pointOfOrigin.Name;
                poi.Labels["EntityType"] = "PointOfOrigin";
                SetPoiLocation(pointOfOrigin.Position, new CourseType(), pointOfOrigin.Time, poi);
                return poi;
            }
            SetPoiLocation(pointOfOrigin.Position, new CourseType(), pointOfOrigin.Time, poi);
            SetPoiEllipse(pointOfOrigin.Area, poi, "128,255,255,0");
            return null;
        }

        /// <summary>
        /// Show the Point of Origin or Impact area as an ellipse
        /// </summary>
        private static void SetPoiEllipse(AreaType area, PoI poi, string fillColor) {
            if (area == null) return;
            poi.Ellipse = new Ellipse(area.MajorAxis, area.MinorAxis, area.Orientation, fillColor);
        }

        private PoI ConvertPointOfImpactToPoI(PointOfImpactType pointOfImpact) {
            var pois = ps.PoIs;
            var poi = pois.FirstOrDefault(p => p.Name.Equals(pointOfImpact.Name)) as PoI;
            if (poi == null) {
                Execute.OnUIThread(() => {
                    poi = CreatePoiFromClassification("PointOfImpact");
                    if (poi == null) return;
                    poi.Name = pointOfImpact.Name;
                    poi.Labels["EntityType"] = "PointOfImpact";
                });
            } 
            SetPoiLocation(pointOfImpact.Position, new CourseType(), pointOfImpact.Time, poi);
            SetPoiEllipse(pointOfImpact.Area, poi, "128,0,0,255");
            return poi;
        }

        private PoI ConvertFireToPoI(FireType fire) {
            // TODO
            //var poi = poiMap.FirstOrDefault(p => p.Key.Equals("Fire", StringComparison.OrdinalIgnoreCase));
            //if (poi == null) poi = PoiTypes[0].Clone() as PoI;
            //else poi = poi.Clone() as PoI;
            //if (poi == null) return null;
            //poi.Name = fire.Name;
            //poi.Labels["EntityType"] = "Fire";
            //SetPoiLocation(fire.Position, poi);
            //return poi;
            var pois = ps.PoIs;
            var poi = pois.FirstOrDefault(p => p.Name.Equals(fire.Name)) as PoI;
            if (poi == null) {
                Execute.OnUIThread(() => {
                    poi = CreatePoiFromClassification("Fire");
                    if (poi == null) return;
                    poi.Name = fire.Name;
                    poi.Labels["EntityType"] = "Fire";
                });
            }
            SetPoiLocation(fire.Position, new CourseType(), fire.Time, poi);
            return poi;
        }

        /// <summary>
        /// Converts a detonation to a Point of Interest
        /// </summary>
        /// <see cref="http://www.inetres.com/gp/military/infantry/mortar/81mm.html"/>
        /// <seealso cref="http://www.inetres.com/gp/military/infantry/mortar/60mm.html"/>
        /// <param name="detonation"></param>
        /// <returns></returns>
        private PoI ConvertDetonationToPoI(DetonationType detonation) {
            var pois = ps.PoIs;
            var poi = pois.FirstOrDefault(p => p.Name.Equals(detonation.Name)) as PoI;
            if (poi == null) {
                Execute.OnUIThread(() => {
                    poi = CreatePoiFromClassification("Explosion");
                    if (poi == null) return;
                    poi.Name = detonation.Name;
                    poi.Labels["EntityType"] = "Explosion";
                });
            }
            SetPoiLocation(detonation.Position, new CourseType(), detonation.Time, poi);
            return poi;
        }

        /// <summary>
        /// Creates a new Point of Interest from the entity type data.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private PoI ConvertEntityToPoI(EntityType entity) {
            var poi = CreatePoiFromClassification(entity.Classification);
            if (poi == null) return null;
            poi.Name = entity.Name;
            switch (entity.Identification) {
                case "FRIENDLY":
                    poi.Style.FillColor = Colors.LightBlue;
                    break;
                case "OPPOSING":
                    poi.Style.FillColor = Colors.Red;
                    break;
                case "NEUTRAL":
                    poi.Style.FillColor = Colors.LightGreen;
                    break;
                default:
                    poi.Style.FillColor = Colors.Yellow;
                    break;
            }
            poi.Labels["EntityType"]         = "Entity";
            poi.Labels["History"]            = "Items=15;Color=Blue;Width=3";
            poi.Labels[EntityId]             = entity.Identification;

            SetPoiLocation(entity.Position, entity.Course, entity.Time, poi);

            //poi.Labels["TimeBased.Model"]    = entity.Classification;
            //if (entity.Course.HeadingSpecified)
            //    poi.Orientation            = entity.Course.Heading;
            return poi;
        }

        private PoI CreatePoiFromClassification(string classification) {
            var lookupValue = classification;
            var poiType = ps.PoITypes.FirstOrDefault(p => string.Equals(p.Name, lookupValue, StringComparison.OrdinalIgnoreCase)) as PoI ??
                          ps.PoITypes.First() as PoI;

            if (poiType == null)
            {
                poiType = new PoI
                {
                    Service = ps,
                    Name = classification,
                    Style = new PoIStyle
                    {
                        CallOutFillColor = Colors.White,
                        CallOutForeground = Colors.Black,
                        TapMode = TapMode.CallOutPopup,
                        DrawingMode = DrawingModes.Point,
                        TitleMode = TitleModes.Bottom,
                        NameLabel = "Name",
                        FillColor = Colors.Green,
                        Icon = "QuestionMarkIcon.png",
                        IconHeight = 23,
                        IconWidth = 23
                    },
                    MetaInfo = new List<MetaInfo> {
                        new MetaInfo { CanEdit = false, Label = "Name",  Title = "Name",           Type = MetaTypes.text,   VisibleInCallOut = true },
                        new MetaInfo { CanEdit = false, Label = "id",    Title = "Identification", Type = MetaTypes.text,   VisibleInCallOut = true },
                        new MetaInfo { CanEdit = false, Label = "[alt]", Title = "Altitude",       Type = MetaTypes.sensor, VisibleInCallOut = true },
                        new MetaInfo { CanEdit = false, Label = "[v]",   Title = "Speed",          Type = MetaTypes.sensor, VisibleInCallOut = true },
                    }
                };
                ps.PoITypes.Add(poiType);
                ps.SaveXml();
            }

            var poi = new PoI
            {
                Service   = ps,
                PoiTypeId = poiType.ContentId,
                PoiType   = poiType,
                Style     = new PoIStyle()
            };
            //poi.UpdateEffectiveStyle();
            return poi;
        }

        /// <summary>
        /// Set the Position of the Point of Interest.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="course"></param>
        /// <param name="time"></param>
        /// <param name="poi"></param>
        private static void SetPoiLocation(PositionType position, CourseType course, DateTime time, BaseContent poi) {
            poi.AddPosition(new Position(position.Longitude, position.Latitude, position.Altitude, course.Speed, course.Heading), time);
            poi.AddSensorData("[alt]", time, position.Altitude);
            poi.AddSensorData("[v]", time, course.Speed);
        }

        #endregion DIS message processing

    }
}