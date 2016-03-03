using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
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
        public const string SERVICENAME = "Tracks";
        public const int PollingIntervalMillis = 5000;

        private static WebMercator _mercator = new WebMercator();

        private AppStateSettings _appState;
        private GraphicsLayer _graphicsLayer;
        private DataServerBase _dataServer;

        public PoiService PoiService { get; private set; }

        public HlaRestService(DataServerBase dataServer, AppStateSettings appState)
        {
            _dataServer = dataServer;
            _appState = appState;

            if (_dataServer == null) return;
            if (PoiService != null) return;

            PoiService = PoiService.CreateService(_dataServer, SERVICENAME, new Guid("EEEEEEEE-487E-4B0A-B3AE-64A0B38855D9"), true, false, true);
            PoiService.HasSensorData = false;
            PoiService.Initialized += (e, f) =>
            {
                PoiService.Settings.TabBarVisible = false;
                PoiService.Settings.AutoStart = true;
                PoiService.Settings.FilterLocation = true;
                PoiService.Settings.SelectionMode = SelectionMode.Single;
                PoiService.Layer.CanStop = false;
                //HlaBridgePoiService.Settings.SelectionMode = SelectionMode.Single;
                //HlaBridgePoiService.Layer.UseClusterer = true;
                //HlaBridgePoiService.SaveXml(true);
            };
        }

        public void Init(string server, List<string> restLabels)//, List<PoI> poiTypes)
        {
            _restLabels = restLabels;
            _graphicsLayer = new GraphicsLayer { ID = SERVICENAME };
            _graphicsLayer.Initialize();

            PoiService.Layer.ChildLayers.Insert(0, _graphicsLayer);
            _appState.ViewDef.UpdateLayers();

            _client = new HttpClient { BaseAddress = new Uri(server) };
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            PoiService.Start();
        }

        private bool _pollPois = false;
        private DateTime _currTimestamp;
        private HttpClient _client;
        private int nPois = 1000;
        private int totalPois = 0;

        private List<string> _restLabels = new List<string>();

        public void StartPollingPois(Action<PoI> poiCallback = null)
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
                    totalPois = 0;

                    var pois = await PollPois(nPois, (poi) =>
                    {
                        totalPois++;
                        var servicePoi = PoiService.PoIs.FirstOrDefault(p => p.PoiId == poi.PoiId);

                        if (servicePoi == null)
                        {
                            if (poi.Position.Latitude != 0 && poi.Position.Longitude != 0)
                            {
                                poiCallback?.Invoke(poi);
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
                                poi.Labels.ForEach(label =>
                                {
                                    var prev = servicePoi.Labels[label.Key];
                                    if (prev != label.Value)
                                    {
                                        servicePoi.Labels[label.Key] = label.Value;
                                        servicePoi.TriggerLabelChanged(label.Key, prev, label.Value);
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

                    PoiService.PoIs.FinishBatch();

                    if (idleDuration > 0)
                    {
                        Debug.WriteLine("Wait for {0:0.0}s", idleDuration / 1000F);
                        await Task.Delay(TimeSpan.FromMilliseconds(idleDuration));
                    }
                }
                var duration = DateTime.Now - startTimestamp;
                Debug.WriteLine("Loading took {0}m", duration.TotalMinutes);
            });
        }

        private async void PoiOnLabelChanged(object sender, LabelChangedEventArgs labelChangedEventArgs)
        {
            var poi = (PoI)sender;
            if (poi.IsNotifying && poi.Labels.ContainsKey("trackId"))
            {
                var entityUpdate = new JObject
                {
                    {"TrackId", poi.Labels["trackId"] },
                    //{"EntityId", poi.ContentId },
                    {"properties", new JObject() }
                };
                var properties = (JObject)entityUpdate["properties"];

                if (!string.IsNullOrWhiteSpace(labelChangedEventArgs.Label) && labelChangedEventArgs.OldValue != labelChangedEventArgs.NewValue)
                {
                    if (_restLabels.Contains(labelChangedEventArgs.Label))
                    {
                        properties[labelChangedEventArgs.Label] = labelChangedEventArgs.NewValue;
                    }
                    else
                    {
                        poi.Labels.ForEach(label =>
                        {
                            if (_restLabels.Contains(label.Key))
                            {
                                properties[label.Key] = label.Value;
                            }
                        });
                    }

                    var body = JsonConvert.SerializeObject(entityUpdate, Formatting.None);
                    // post to rest service
                    await _client.PutAsync($"api/HlaEntities/{poi.Labels["trackId"]}", new StringContent(body));
                    //await _client.PutAsync($"api/HlaEntities/{poi.ContentId}", new StringContent(body));
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
            try
            {
                var response = await _client.GetAsync("api/HlaEntities");
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return new PoI[0];
                }
                var body = await response.Content.ReadAsStringAsync();

                var jBody = JObject.Parse(body);

                var features = jBody["features"].OfType<JObject>();
                var pois = features.Select(f => ParseFeature(f, parseFeatureCallback)).ToArray();

                return pois;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return new PoI[0];
            }
        }

        protected PoI ParseFeature(JObject feature, Action<PoI> callback)
        {
            var poi = new PoI();
            var prevNotifying = poi.IsNotifying;
            poi.IsNotifying = false;
            poi.PoiId = (string)feature["properties"]["entityIdentifier"];//Guid.Parse((string)feature["properties"]["id"]);
            //poi.Name = (string) feature["properties"]["Name"];
            //poi.ContentId = (string) feature["properties"]["Bame"];
            poi.Service = PoiService;
            poi.Position = new Position((float)feature["geometry"]["coordinates"][0],
                (float)feature["geometry"]["coordinates"][1]);
            var featureTypeId = (string)feature["properties"]["featureTypeId"];
            var pt = PoiService.PoITypes.FirstOrDefault(poiType => string.Equals(poiType.ContentId, featureTypeId));
            poi.PoiTypeId = pt == null ? PoiService.PoITypes[0].ContentId : pt.ContentId;
            poi.IsNotifying = prevNotifying;

            feature["properties"].OfType<JProperty>().ForEach(prop =>
            {
                //if (!_restLabels.Contains(prop.Name))
                //{
                //    _restLabels.Add(prop.Name);
                //}
                poi.Labels.Add(prop.Name, (string)prop.Value);
            });
            //poi.Labels.Add("speed", (string)feature["properties"]["speed"]);

            callback?.Invoke(poi);

            return poi;
        }

    }
}
