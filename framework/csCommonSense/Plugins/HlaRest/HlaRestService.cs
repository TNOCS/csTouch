using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Media;
using csGeoLayers;
using csShared;
using Caliburn.Micro;
using DataServer;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Projection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Position = DataServer.Position;
using Task = System.Threading.Tasks.Task;

namespace csCommon.Plugins.HlaRest
{
    public class HlaRestService
    {
        public const string SERVICENAME = "HLA";
        public const int PollingIntervalMillis = 0;

        private static WebMercator _mercator = new WebMercator();

        private AppStateSettings _appState;
        private GraphicsLayer _graphicsLayer;
        private DataServerBase _dataServer;

        public PoiService PoiService { get; private set; }
        private PoI _poiType;

        public HlaRestService(DataServerBase dataServer, AppStateSettings appState)
        {
            _dataServer = dataServer;
            _appState = appState;
        }

        public void Init(string server)
        {
            if (_dataServer == null) return;
            if (PoiService != null) return;

            PoiService = PoiService.CreateService(_dataServer, SERVICENAME, Guid.NewGuid(), true, false, true);
            var poiTypes = new List<PoI>();

            var pt = PoiService.AddPoiType("NEUTRAL_HUMAN", DrawingModes.Image, Colors.White, Colors.Black, 2, "UserIdentity_NeutralHuman.png", 15);
            pt.AddMetaInfo("Speed", "speed", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}", false, false);
            pt.AddMetaInfo("Altitude", "altitude", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}", false, false);
            pt.AddMetaInfo("TrackType", "trackType", MetaTypes.text, false, true, "Info", null, double.NaN, double.NaN, "{0}", false, true);
            poiTypes.Add(pt);

            pt = PoiService.AddPoiType("NEUTRAL_VEHICLE_SMALL", DrawingModes.Image, Colors.White, Colors.Black, 2, "UserIdentity_Neutral.png", 15);
            pt.AddMetaInfo("Speed", "speed", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}", false, false);
            pt.AddMetaInfo("Altitude", "altitude", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}", false, false);
            pt.AddMetaInfo("TrackType", "trackType", MetaTypes.text, false, true, "Info", null, double.NaN, double.NaN, "{0}", false, true);
            poiTypes.Add(pt);

            pt = PoiService.AddPoiType("NEUTRAL_AIRCRAFT", DrawingModes.Image, Colors.White, Colors.Black, 2, "track.png", 15);
            pt.AddMetaInfo("Speed", "speed", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}", false, false);
            pt.AddMetaInfo("Altitude", "altitude", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}", false, false);
            pt.AddMetaInfo("TrackType", "trackType", MetaTypes.text, false, true, "Info", null, double.NaN, double.NaN, "{0}", false, true);
            pt.AddMetaInfo("Test", "testLabel", MetaTypes.text, false, true, "Info", null, double.NaN, double.NaN, "{0}", false, true);
            poiTypes.Add(pt);

            pt = PoiService.AddPoiType("NEUTRAL_VEHICLE_SMALL", DrawingModes.Image, Colors.White, Colors.Black, 2, "UserIdentity_Neutral.png", 15);
            pt.AddMetaInfo("Speed", "speed", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}", false, false);
            pt.AddMetaInfo("Altitude", "altitude", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}", false, false);
            pt.AddMetaInfo("TrackType", "trackType", MetaTypes.text, false, true, "Info", null, double.NaN, double.NaN, "{0}", false, true);
            poiTypes.Add(pt);

            pt = PoiService.AddPoiType("FRIENDLY_HUMAN", DrawingModes.Image, Colors.White, Colors.Black, 2, "UserIdentity_FriendlyHuman.png", 30);
            pt.AddMetaInfo("Speed", "speed", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}", false, false);
            pt.AddMetaInfo("Altitude", "altitude", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}", false, false);
            pt.AddMetaInfo("TrackType", "trackType", MetaTypes.text, false, true, "Info", null, double.NaN, double.NaN, "{0}", false, true);
            poiTypes.Add(pt);

            pt = PoiService.AddPoiType("FRIENDLY_VEHICLE_SMALL", DrawingModes.Image, Colors.White, Colors.Black, 2, "UserIdentity_Friendly.png", 30);
            pt.AddMetaInfo("Speed", "speed", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}", false, false);
            pt.AddMetaInfo("Altitude", "altitude", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}", false, false);
            pt.AddMetaInfo("TrackType", "trackType", MetaTypes.text, false, true, "Info", null, double.NaN, double.NaN, "{0}", false, true);
            poiTypes.Add(pt);

            pt = PoiService.AddPoiType("FRIENDLY_AIRCRAFT", DrawingModes.Image, Colors.White, Colors.Black, 2, "track.png", 15);
            pt.AddMetaInfo("Speed", "speed", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}", false, false);
            pt.AddMetaInfo("Altitude", "altitude", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}", false, false);
            pt.AddMetaInfo("TrackType", "trackType", MetaTypes.text, false, true, "Info", null, double.NaN, double.NaN, "{0}", false, true);
            pt.AddMetaInfo("Test", "testLabel", MetaTypes.text, false, true, "Info", null, double.NaN, double.NaN, "{0}", false, true);
            poiTypes.Add(pt);

            pt = PoiService.AddPoiType("HOSTILE_HUMAN", DrawingModes.Image, Colors.White, Colors.Black, 2, "UserIdentity_HostileHuman.png", 15);
            pt.AddMetaInfo("Speed", "speed", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}", false, false);
            pt.AddMetaInfo("Altitude", "altitude", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}", false, false);
            pt.AddMetaInfo("TrackType", "trackType", MetaTypes.text, false, true, "Info", null, double.NaN, double.NaN, "{0}", false, true);
            poiTypes.Add(pt);

            pt = PoiService.AddPoiType("HOSTILE_VEHICLE_SMALL", DrawingModes.Image, Colors.White, Colors.Black, 2, "UserIdentity_Hostile.png", 15);
            pt.AddMetaInfo("Speed", "speed", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}");
            pt.AddMetaInfo("Altitude", "altitude", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}", false, false);
            pt.AddMetaInfo("TrackType", "trackType", MetaTypes.text, false, true, "Info", null, double.NaN, double.NaN, "{0}", false, true);
            poiTypes.Add(pt);

            pt = PoiService.AddPoiType("HOSTILE_AIRCRAFT", DrawingModes.Image, Colors.White, Colors.Black, 2, "track.png", 15);
            pt.AddMetaInfo("Speed", "speed", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}", false, false);
            pt.AddMetaInfo("Altitude", "altitude", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}", false, false);
            pt.AddMetaInfo("TrackType", "trackType", MetaTypes.text, false, true, "Info", null, double.NaN, double.NaN, "{0}", false, true);
            poiTypes.Add(pt);

            pt = PoiService.AddPoiType("PENDING_HUMAN", DrawingModes.Image, Colors.White, Colors.Black, 2, "UserIdentity_PendingHuman.png", 15);
            pt.AddMetaInfo("Speed", "speed", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}", false, false);
            pt.AddMetaInfo("Altitude", "altitude", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}", false, false);
            pt.AddMetaInfo("TrackType", "trackType", MetaTypes.text, false, true, "Info", null, double.NaN, double.NaN, "{0}", false, true);
            poiTypes.Add(pt);

            pt = PoiService.AddPoiType("PENDING_VEHICLE_SMALL", DrawingModes.Image, Colors.White, Colors.Black, 2, "UserIdentity_Pending.png", 15);
            pt.AddMetaInfo("Speed", "speed", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}", false, false);
            pt.AddMetaInfo("Altitude", "altitude", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}", false, false);
            pt.AddMetaInfo("TrackType", "trackType", MetaTypes.text, false, true, "Info", null, double.NaN, double.NaN, "{0}", false, true);
            poiTypes.Add(pt);

            pt = PoiService.AddPoiType("PENDING_AIRCRAFT", DrawingModes.Image, Colors.White, Colors.Black, 2, "track.png", 15);
            pt.AddMetaInfo("Speed", "speed", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}", false, false);
            pt.AddMetaInfo("Altitude", "altitude", MetaTypes.number, false, true, "Info", null, 0, 500, "{0:N0}", false, false);
            pt.AddMetaInfo("Type", "trackType", MetaTypes.text, false, true, "Info", null, double.NaN, double.NaN, "{0}", false, true);
            pt.AddMetaInfo("Test", "testLabel", MetaTypes.text, false, true, "Info", null, double.NaN, double.NaN, "{0}", false, true);
            poiTypes.Add(pt);

            _graphicsLayer = new GraphicsLayer {ID = SERVICENAME};
            _graphicsLayer.Initialize();

            PoiService.Layer.ChildLayers.Insert(0, _graphicsLayer);
            _appState.ViewDef.UpdateLayers();


            _client = new HttpClient {BaseAddress = new Uri(server)};
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            PoiService.Start();
        }

        private bool _pollPois = false;
        private DateTime _currTimestamp;
        private HttpClient _client;
        private int nPois = 1000;
        private int totalPois = 0;

        private List<string> _restLabels = new List<string>();

        public void StartPollingPois()
        {
            if (_pollPois) return;

            _pollPois = true;
            Task.Run(async () =>
            {
                var startTimestamp = DateTime.Now;
                while (_pollPois)
                {
                    _currTimestamp = DateTime.Now;
                    PoiService.PoIs.StartBatch();

                    var pois = await PollPois(nPois, (poi) =>
                    {
                        var servicePoi = PoiService.PoIs.FirstOrDefault(p => p.PoiId == poi.PoiId);

                        if (servicePoi == null)
                        {
                            if (poi.Position.Latitude != 0 && poi.Position.Longitude != 0)
                            {
                                poi.LabelChanged += PoiOnLabelChanged;
                                PoiService.PoIs.Add(poi);
                            }
                        }
                        else
                        {
                            if (servicePoi.Position.Latitude != poi.Position.Latitude && servicePoi.Position.Longitude != poi.Position.Longitude)
                            {
                                servicePoi.Position = poi.Position;                                
                            }


                            Execute.OnUIThread(() =>
                            {
                                var prevNotifying = poi.IsNotifying;
                                servicePoi.IsNotifying = false;
                                poi.Labels.ForEach((kvp) =>
                                {
                                    var prev = servicePoi.Labels[kvp.Key];
                                    if (prev != kvp.Value)
                                    {
                                        servicePoi.Labels[kvp.Key] = kvp.Value;

                                        servicePoi.TriggerLabelChanged(kvp.Key, prev, kvp.Value);
                                    }
                                });
                                servicePoi.IsNotifying = prevNotifying;
                            });

                        }
                    });

                    var cycleDuration = DateTime.Now - _currTimestamp;
                    Debug.WriteLine("{0} pois loaded ({1}s)", totalPois, cycleDuration.TotalSeconds);

                    if (totalPois >= 2000)
                    {
                        _pollPois = false;
                        Debug.WriteLine("Finished loading {0} pois from rest service", totalPois);
                    }


                    var idleDuration = PollingIntervalMillis - (int)cycleDuration.TotalMilliseconds;

                    if (idleDuration > 0)
                    {
                        Debug.WriteLine("Wait for {0}s", idleDuration/1000);
                        await Task.Delay(TimeSpan.FromMilliseconds(idleDuration));
                    }
                    PoiService.PoIs.FinishBatch();

                }
                var duration = DateTime.Now - startTimestamp;
                Debug.WriteLine("Loading took {0}m", duration.TotalMinutes);
            });
        }

        private async void PoiOnLabelChanged(object sender, LabelChangedEventArgs labelChangedEventArgs)
        {
            var poi = (PoI) sender;
            if (poi.IsNotifying)
            {
                var entityUpdate = new JObject
                {
                    {"EntityId", poi.ContentId },
                    {"properties", new JObject() }
                };
                var properties = (JObject)entityUpdate["properties"];

                if (!string.IsNullOrWhiteSpace(labelChangedEventArgs.Label) && labelChangedEventArgs.OldValue != labelChangedEventArgs.NewValue)
                {
                    if (!_restLabels.Contains(labelChangedEventArgs.Label))
                    {
                        properties[labelChangedEventArgs.Label] = labelChangedEventArgs.NewValue;
                    }
                    else
                    {
                        poi.Labels.ForEach(label =>
                        {
                            if (!_restLabels.Contains(label.Key))
                            {
                                properties[label.Key] = label.Value ;
                            }
                        });
                    }

                    var body = JsonConvert.SerializeObject(entityUpdate, Formatting.None);
                    // post to rest service
                    await _client.PutAsync($"api/HlaEntities/{poi.ContentId}", new StringContent(body));
                }

                
            }
        }

        public void StopPollingPois()
        {
            _pollPois = false;
        }

        public async Task<PoI[]> PollPois(int nPois = 5, Action<PoI> parseFeatureCallback = null)
        {
            //var response = await _client.GetAsync("pois?npois=" + nPois);
            var response = await _client.GetAsync("api/HlaEntities");
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return new PoI[0];
            }
            var body = await response.Content.ReadAsStringAsync();

            var jBody = JObject.Parse(body);

            var features = jBody["features"].OfType<JObject>();
            var pois = features.Select(f=>ParseFeature(f, parseFeatureCallback)).ToArray();

            return pois;
        }

        protected PoI ParseFeature(JObject feature, Action<PoI> callback)
        {
            var poi = new PoI();
            var prevNotifying = poi.IsNotifying;
            poi.IsNotifying = false;
            poi.PoiId = (string) feature["properties"]["entityIdentifier"];//Guid.Parse((string)feature["properties"]["id"]);
            //poi.Name = (string) feature["properties"]["Name"];
            //poi.ContentId = (string) feature["properties"]["Bame"];
            poi.Service = PoiService;
            poi.Position = new Position((float) feature["geometry"]["coordinates"][0],
                (float) feature["geometry"]["coordinates"][1]);
            poi.PoiTypeId = (string)feature["properties"]["featureTypeId"];
            poi.IsNotifying = prevNotifying;

            feature["properties"].OfType<JProperty>().ForEach(prop =>
            {
                if (!_restLabels.Contains(prop.Name))
                {
                    _restLabels.Add(prop.Name);
                }
                poi.Labels.Add(prop.Name, (string)prop.Value);
            });
            //poi.Labels.Add("speed", (string)feature["properties"]["speed"]);
            

            callback?.Invoke(poi);

            return poi;
        }

    }
}
