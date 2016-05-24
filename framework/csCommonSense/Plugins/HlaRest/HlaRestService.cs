using System;
using System.Collections.Concurrent;
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
//using ESRI.ArcGIS.Client.Projection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Position = DataServer.Position;
using Task = System.Threading.Tasks.Task;

namespace csCommon.Plugins.HlaRest
{
    public class HlaRestService
    {
        public const string ServiceName = "Tracks";
        private readonly int pollingIntervalMillis = 1000 * AppStateSettings.Instance.Config.GetInt("REST.PollingIntervalInSeconds", 5);
        private readonly ConcurrentDictionary<string, DateTime> poiUpdateTimes = new ConcurrentDictionary<string, DateTime>();
        private readonly TimeSpan poiKeepAliveTime = new TimeSpan(0, 0, AppStateSettings.Instance.Config.GetInt("REST.PoiKeepAliveTimeInSeconds", 5));

        //private static WebMercator _mercator = new WebMercator();

        private readonly AppStateSettings appState;
        private GraphicsLayer graphicsLayer;
        private readonly DataServerBase dataServer;
        public PoiService PoiService { get; }

        public HlaRestService(DataServerBase dataServer, AppStateSettings appState)
        {
            this.dataServer = dataServer;
            this.appState = appState;

            if (this.dataServer == null) return;
            if (PoiService != null) return;

            PoiService = PoiService.CreateService(this.dataServer, ServiceName, new Guid("EEEEEEEE-487E-4B0A-B3AE-64A0B38855D9"), true);
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

        /// <summary>
        /// Initialize the service and specify which labels can be edited by the user.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="editableRestLabels"></param>
        public void Init(string server, List<string> editableRestLabels)//, List<PoI> poiTypes)
        {
            restLabels = editableRestLabels;
            graphicsLayer = new GraphicsLayer { ID = ServiceName };
            graphicsLayer.Initialize();

            PoiService.Layer.ChildLayers.Insert(0, graphicsLayer);
            appState.ViewDef.UpdateLayers();

            client = new HttpClient { BaseAddress = new Uri(server) };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            PoiService.Start();
        }

        private bool pollPois;
        private DateTime currTimestamp;
        private HttpClient client;
        // private const int NumberOfPois = 1000;
        private const double Tolerance = 0.000001;
        private int totalPois;

        private List<string> restLabels = new List<string>();

        public void StartPollingPois(Action<PoI> poiCallback = null)
        {
            if (pollPois) return;

            pollPois = true;
            Task.Run(async () =>
            {
                // var startTimestamp = DateTime.Now;
                while (pollPois)
                {
                    currTimestamp = DateTime.Now;
                    PoiService.PoIs.StartBatch();
                    totalPois = 0;
                    var now = DateTime.Now;
                    //var poisToUpdate = new List<dynamic>();

                    await PollPois(poi =>
                    {
                        totalPois++;
                        var servicePoi = PoiService.PoIs.FirstOrDefault(p => p.PoiId == poi.PoiId);

                        if (servicePoi == null)
                        {
                            if (Math.Abs(poi.Position.Latitude) < Tolerance || Math.Abs(poi.Position.Longitude) < Tolerance) return;
                            poiCallback?.Invoke(poi);
                            poi.LabelChanged += PoiOnLabelChanged;
                            PoiService.PoIs.Add(poi);
                            poiUpdateTimes[poi.PoiId] = now;
                        }
                        else
                        {
                            poiUpdateTimes[servicePoi.PoiId] = now;

                            if (Math.Abs(servicePoi.Position.Latitude - poi.Position.Latitude) > Tolerance && Math.Abs(servicePoi.Position.Longitude - poi.Position.Longitude) > Tolerance)
                            {
                                servicePoi.Position = poi.Position;
                            }

                            Execute.OnUIThread(() =>
                            {
                                var prevNotifying = servicePoi.IsNotifying;
                                servicePoi.IsNotifying = false;
                                poi.Labels.ForEach(label =>
                                {
                                    var prev = servicePoi.Labels[label.Key];
                                    if (prev == label.Value) return;
                                    servicePoi.Labels[label.Key] = label.Value;
                                    servicePoi.TriggerLabelChanged(label.Key, prev, label.Value);
                                });
                                servicePoi.IsNotifying = prevNotifying;
                            });
                        }
                    });
                    PoiService.PoIs.FinishBatch();

                    //Execute.OnUIThread(() =>
                    //{
                    //    poisToUpdate.ForEach(pois =>
                    //    {
                    //        var servicePoi = pois.ServicePoi as BaseContent;
                    //        var poi = pois.BasePoi as PoI;

                    //        if (poi == null || servicePoi == null) return;

                    //        var prevNotifying = servicePoi.IsNotifying;
                    //        servicePoi.IsNotifying = false;
                    //        poi.Labels.ForEach(label =>
                    //        {
                    //            var prev = servicePoi.Labels[label.Key];
                    //            if (prev == label.Value) return;
                    //            servicePoi.Labels[label.Key] = label.Value;
                    //            servicePoi.TriggerLabelChanged(label.Key, prev, label.Value);
                    //        });
                    //        servicePoi.IsNotifying = prevNotifying;
                    //    });
                    //});

                    var cycleDuration = DateTime.Now - currTimestamp;
                    Debug.WriteLine("{0} pois loaded ({1}s)", totalPois, cycleDuration.TotalSeconds);

                    if (totalPois >= 2000)
                    {
                        pollPois = false;
                        Debug.WriteLine("Finished loading {0} pois from rest service", totalPois);
                    }

                    var idleDuration = pollingIntervalMillis - (int)cycleDuration.TotalMilliseconds;

                    RemoveOldPois();


                    if (idleDuration <= 0) continue;
                    Debug.WriteLine("Wait for {0:0.0}s", idleDuration / 1000F);
                    await Task.Delay(TimeSpan.FromMilliseconds(idleDuration));
                }
                // var duration = DateTime.Now - startTimestamp;
                // Debug.WriteLine("Loading took {0}m", duration.TotalMinutes);
            });
        }

        /// <summary>
        /// Remove POIs that haven't been updated for some time.
        /// </summary>
        private void RemoveOldPois()
        {
            var now = DateTime.Now;
            DateTime dt;
            var poisToRemove = new List<BaseContent>();
            poiUpdateTimes.ForEach(kvp =>
            {
                if (now - kvp.Value < poiKeepAliveTime) return;
                var poi = PoiService.PoIs.FirstOrDefault(p => p.PoiId == kvp.Key);
                if (poi == null) return;
                poiUpdateTimes.TryRemove(kvp.Key, out dt);
                poisToRemove.Add(poi);
                poi.LabelChanged -= PoiOnLabelChanged;
            });
            if (poisToRemove.Count == 0) return;
            Execute.OnUIThread(() =>
            {
                // Debug.WriteLine(">>> Deleting {0} POIs.", poisToRemove.Count);
                PoiService.PoIs.StartBatch();
                foreach (var poi in poisToRemove)
                {
                    PoiService.PoIs.Remove(poi);
                }
                PoiService.PoIs.FinishBatch();
            });
        }

        private async void PoiOnLabelChanged(object sender, LabelChangedEventArgs labelChangedEventArgs)
        {
            var poi = (PoI)sender;
            if (!poi.IsNotifying || !poi.Labels.ContainsKey("hlaIdentifier")) return;
            var entityUpdate = new JObject
            {
                {"HlaIdentifier", poi.Labels["hlaIdentifier"] },
                //{"EntityId", poi.ContentId },
                {"properties", new JObject() }
            };
            var properties = (JObject)entityUpdate["properties"];

            if (string.IsNullOrWhiteSpace(labelChangedEventArgs.Label) ||
                labelChangedEventArgs.OldValue == labelChangedEventArgs.NewValue) return;
            if (restLabels.Contains(labelChangedEventArgs.Label))
            {
                properties[labelChangedEventArgs.Label] = labelChangedEventArgs.NewValue;
            }
            else
            {
                poi.Labels.ForEach(label =>
                {
                    if (restLabels.Contains(label.Key))
                    {
                        properties[label.Key] = label.Value;
                    }
                });
            }

            var body = JsonConvert.SerializeObject(entityUpdate, Formatting.None);
            // post to rest service
            await client.PutAsync($"api/HlaEntities/{poi.Labels["hlaIdentifier"]}", new StringContent(body));
            //await _client.PutAsync($"api/HlaEntities/{poi.ContentId}", new StringContent(body));
        }

        public void StopPollingPois()
        {
            pollPois = false;
        }

        public async Task<PoI[]> PollPois(Action<PoI> parseFeatureCallback = null)
        {
            //var response = await _client.GetAsync("pois?npois=" + NumberOfPois);
            try
            {
                var response = await client.GetAsync("api/HlaEntities");
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
            poi.PoiId = (string)feature["properties"]["hlaIdentifier"];//Guid.Parse((string)feature["properties"]["id"]);
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
