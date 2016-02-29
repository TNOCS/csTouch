using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using csGeoLayers;
using csShared;
using csShared.Geo;
using csShared.Utils;
using csWebDotNetLib;
using Caliburn.Micro;
using DataServer;
using IO.Swagger.Model;
using Newtonsoft.Json.Linq;
using Geometry = IO.Swagger.Model.Geometry;
using MenuItem = System.Windows.Controls.MenuItem;

namespace csCommon.Types.DataServer.PoI
{
    // TODO What is the distinction between the regular PoiService and this class? In other words, why are the methods below not in PoiService?
    public class WebApiService : PoiService
    {
        protected static readonly AppStateSettings AppState = AppStateSettings.Instance;

        private readonly List<string> availableColors = new List<string>
        {
            "Transparent",
            "Red",
            "Orange",
            "Yellow",
            "Black",
            "White",
            "Green",
            "Blue",
            "Purple",
            "Pink"
        };

        protected csWebApi api;
        protected Color ShapeAccentColor = Colors.Red;
        protected Subscription sub;
        public string File { get; set; }
        private Resource resource;

        public string DsdFile
        {
            get { return Path.ChangeExtension(File, "dsd"); }
        }

        private Layer cslayer;

        public Layer csLayer
        {
            get { return cslayer; }
            set { cslayer = value; }
        }


        // SdJ Added so OpenFile can send an event whenever the file is opened.
        public event EventHandler FileOpenedEvent;

        public static WebApiService CreateWebApiService(csWebApi api, string name, Layer layer, Guid id, string folder = "",
            string relativeFolder = "")
        {

            var res = new WebApiService
            {
                IsLocal = true,
                Name = layer.Title,
                Id = id,
                IsFileBased = false,
                StaticService = true,
                AutoStart = false,
                HasSensorData = false,
                Mode = Mode.client,
                RelativeFolder = relativeFolder,
                api = api
            };
            res.csLayer = layer;
            res.Init(Mode.client, AppState.DataServer);
            res.Folder = folder;
            res.InitPoiService();

            res.Settings.OpenTab = false;
            res.Settings.Icon = "layer.png";
            AppState.DataServer.Services.Add(res);
            return res;
        }

        /// <summary>
        ///     Stops and then starts the service again.
        /// </summary>
        private void RestartService()
        {
            var ls = Layer as IStartStopLayer;
            if (ls == null || !ls.IsStarted) return;
            ls.Stop();
            ls.Start();
        }

        public static Feature GetFeature(JObject o)
        {
            var os = o.ToString();
            var r = JSONHelper.Deserialize<Feature>(os);
            return r;
        }

        public static Feature GetFeatureFromPoi(global::DataServer.PoI p)
        {
            var f = new Feature();
            f.Id = p.Id.ToString();
            f.Geometry = new Geometry();
            f.Type = "Feature";
            switch (p.NEffectiveStyle.DrawingMode.ToString())
            {
                case "Polyline":
                    f.Geometry.Type = "LineString";
                    f.Geometry.Coordinates = new JArray(p.Points.Select(point=>new JArray() {point.X, point.Y}));
                    break;
                case "Polygon":
                    f.Geometry.Type = "Polygon";
                    f.Geometry.Coordinates = new JArray();
                    ((JArray) f.Geometry.Coordinates).Add(
                        new JArray(p.Points.Select(point => new JArray() {point.X, point.Y})));
                    break;
                default:
                    f.Geometry.Type = "Point";
                    f.Geometry.Coordinates = new JArray() { p.Position.Longitude, p.Position.Latitude };
                    break;
            }
            


            f.Properties = new Dictionary<string, object>();
            f.Properties["featureTypeId"] = p.PoiTypeId;
            foreach (var l in p.Labels)
            {
                f.Properties[l.Key] = l.Value;
            }

            //f.Geometry = new IO.Swagger.Model.Geometry();


            return f;
        }

        private List<string> availablePoIs = new List<string>();

        private Color GetColor(string s)
        {
            if (string.IsNullOrEmpty(s)) return Colors.Transparent;
            return Colors.Red;
        }

        public static void SyncPoi(global::DataServer.PoI p, Layer l, Feature f, csWebApi api)
        {
            if (!p.Data.ContainsKey("cs") || p.Data["cs"] == null)
            {
                p.Data["cs"] = f;
            }
            var posChanged = Observable.FromEventPattern<PositionEventArgs>(ev => p.PositionChanged += ev,
                ev => p.PositionChanged -= ev);
            posChanged.Throttle(TimeSpan.FromSeconds(1)).Subscribe(k =>
            {
                var coords = f.Geometry.Coordinates as JArray;
                if (coords != null && ((double)coords[0] != p.Position.Longitude || (double)coords[1] != p.Position.Latitude))
                {
                    ((JArray)f.Geometry.Coordinates)[0] = p.Position.Longitude;
                    ((JArray)f.Geometry.Coordinates)[1] = p.Position.Latitude;
                    //var c = ((JArray) f.Geometry.Coordinates).Select(x => (double) x).ToList();
                    //c[0] = p.Position.Longitude;
                    //c[1] = p.Position.Latitude;
                    //f.Geometry.Coordinates = c;
                    var t = api.features.UpdateFeatureAsync(f, l.Id, f.Id);
                }
            });
        }

        public override void Subscribe(Mode serviceMode)
        {
            base.Subscribe(serviceMode);
            Reset();

            PoIs.StartBatch();

            //var isLoaded = LoadOrCreateDataServiceDescription(saveDsd);
            IsLoading = true;
            try
            {
                var layer = api.layers.GetLayer(this.csLayer.Id);
                if (layer.TypeUrl.Length > 0) layer.TypeUrl = layer.TypeUrl.TrimStart('/');
                this.IsTimelineEnabled = true;



                if (!string.IsNullOrEmpty(layer.TypeUrl))
                {
                    if (layer.TypeUrl.StartsWith("api/resources/") || layer.TypeUrl.StartsWith("data/resourceTypes/"))
                    {
                        var r = layer.TypeUrl.Replace("api/resources/", "");
                        r = r.Replace("data/resourceTypes/", "");
                        r = r.Replace(".json", "");

                        var template = this.dsb.Templates.FirstOrDefault(t => t.Name == r);
                        if (template == null)
                        {
                            resource = api.resources.GetResource(r);
//                            var ps = new PoiService();
//                            ps.Name = r;
//                            ps.IsTemplate = true;
//                            ps.InitPoiService();
                            //dsb.Templates.Add(ps);
                            foreach (var ft in resource.FeatureTypes)
                            {
                                var st = ft.Value.Style;


                                var p = new global::DataServer.PoI
                                {
                                    ContentId = ft.Key,
                                    PoiId = ft.Key,
                                    Service = this,
                                    Id = Guid.NewGuid(),
                                    Style = new PoIStyle
                                    {
                                        DrawingMode = DrawingModes.Point,
                                        Icon = AppState.Config.Get("csWebApiServer", "http://localhost:3002") + "/" + ft.Value.Style.IconUri,
                                        //Picture = new BitmapImage(new Uri("http://localhost:3002/" + ft.Value.Style.IconUri))m
                                        CallOutFillColor = Colors.LightBlue,
                                        CallOutForeground = Colors.Black,
                                        CallOutTimeOut = 10,
                                        CallOutOrientation = CallOutOrientation.RightSideMenu,
                                        FillOpacity= ft.Value.Style.FillOpacity,
                                        StrokeColor= (Color)ColorConverter.ConvertFromString(ft.Value.Style.StrokeColor),
                                        StrokeOpacity = ft.Value.Style.Opacity,
                                    },
                                    MetaInfo = new List<MetaInfo>()
                                };

                                if (ft.Value.Style.FillColor != null)
                                {
                                    p.Style.FillColor =
                                        (Color) ColorConverter.ConvertFromString(ft.Value.Style.FillColor);
                                }
                                if (ft.Value.Style.StrokeColor != null)
                                {
                                    p.Style.StrokeColor = (Color)ColorConverter.ConvertFromString(ft.Value.Style.StrokeColor);
                                }
                                if (!string.IsNullOrWhiteSpace(p.Style.Icon))
                                {
                                    p.Style.Picture = new BitmapImage(p.Style.IconUri);
                                }
                                if (!string.IsNullOrEmpty(st.NameLabel)) p.Style.NameLabel = st.NameLabel;

                                if (ft.Value.PropertyTypeKeys != null) {
                                    var properties = ft.Value.PropertyTypeKeys.Split(';');
                                    if (properties.Length > 0)
                                    {
                                        foreach (var prop in properties)
                                        {
                                            if (resource.PropertyTypeData.ContainsKey(prop))
                                            {
                                                var rp = resource.PropertyTypeData[prop];
                                                var mi = new MetaInfo()
                                                {
                                                    Label = rp.Label,
                                                    Description = rp.Description,
                                                    VisibleInCallOut = true,
                                                    IsSearchable = true,
                                                    Section = rp.Section,
                                                    IsEditable = rp.CanEdit ?? true,
                                                    Title = rp.Title
                                                };
                                                switch (rp.Type.ToLower())
                                                {
                                                    case "string":
                                                        mi.Type = MetaTypes.text;
                                                        break;
                                                    case "number":
                                                        mi.Type = MetaTypes.number;
                                                        break;
                                                    case "date":
                                                        mi.Type = MetaTypes.datetime;
                                                        break;
                                                }
                                                p.MetaInfo.Add(mi);
                                            }

                                        }
                                    }
                                }

                                if (PoITypes.All(pt => pt.ContentId != p.ContentId))
                                    this.PoITypes.Add(p);
                                //ps.PoITypes.Add(p);

                            }

                        }


                    }
                    if (layer.TypeUrl.StartsWith(api.client.BasePath))
                    {
                        //api.resources.GetResource()    
                    }

                }
                this.PoIs.CollectionChanged += (es, tp) =>
                {

                    if (tp.NewItems != null && IsInitialized)
                    {
                        foreach (global::DataServer.PoI p in tp.NewItems)
                        {
                            var newp = (!availablePoIs.Contains(p.Id.ToString()));
                            if (newp)
                            {
                                if (IsInitialized)
                                {
                                    var f = GetFeatureFromPoi(p);
                                    WebApiService.SyncPoi(p, layer, f, api);
                                    api.features.AddFeature(layer.Id, f);
                                }
                                availablePoIs.Add(p.Id.ToString());
                            }


                        }
                        // foreach (va)
                    }
                };
                sub = api.GetLayerSubscription(layer.Id);
                sub.LayerCallback += (e, s) =>
                {
                    Execute.OnUIThread(()=> {
                        switch (s.action)
                        {
                            case LayerUpdateAction.deleteFeature:

                                var dp =
                                    PoIs.FirstOrDefault(
                                        k => k.Data.ContainsKey("cs") && ((Feature)k.Data["cs"]).Id == s.featureId);
                                if (dp != null)
                                {
                                    RemovePoi(dp);
                                }
                                break;
                            case LayerUpdateAction.updateFeature:
                                var f = GetFeature((JObject)s.item);
                                // find feature
                                var p = PoIs.FirstOrDefault(k => k.Data.ContainsKey("cs") && ((Feature)k.Data["cs"]).Id == f.Id);
                                if (p != null)
                                {
                                    // update poi
                                    UpdateFeature(f, (global::DataServer.PoI)p, layer);
                                    //TriggerContentChanged(p);
                                }
                                else
                                {
                                    // add poi  
                                    var g = Guid.NewGuid();
                                    availablePoIs.Add(g.ToString());
                                    var np = AddFeature(f, g, layer);

                                }
                                break;
                        }
                    });
                };
                foreach (var f in layer.Features)
                {
                    var p = AddFeature(f, Guid.NewGuid(), layer);
                    availablePoIs.Add(p.Id.ToString());
                }


                IsLoading = false;

                ContentLoaded = true;

                Execute.OnUIThread(() => Layer.IsLoading = false);
                PoIs.FinishBatch();
            }
            catch (Exception e)
            {
                AppState.TriggerNotification(e.Message);
            }


        }

        private global::DataServer.PoI AddFeature(Feature f, Guid id, Layer layer)
        {
            var p = new global::DataServer.PoI { Service = this, Id = id, PoiTypeId = f.Properties.ContainsKey("featureTypeId") ?(string)f.Properties["featureTypeId"]:null };
            UpdateFeature(f, p, layer);


            PoIs.Add(p);
            p.Deleted += (o, s) => { if (IsInitialized) api.features.DeleteFeature(f.Id, layer.Id); };

            var posChanged = Observable.FromEventPattern<PositionEventArgs>(ev => p.PositionChanged += ev,
                ev => p.PositionChanged -= ev);
            posChanged.Throttle(TimeSpan.FromSeconds(1)).Subscribe(k =>
            {
                if (f.Geometry.Coordinates is JArray)
                {
                    var coords = f.Geometry.Coordinates as JArray;
                    if (coords != null &&
                        ((double) coords[0] != p.Position.Longitude || (double) coords[1] != p.Position.Latitude))
                    {
                        ((JArray) f.Geometry.Coordinates)[0] = p.Position.Longitude;
                        ((JArray) f.Geometry.Coordinates)[1] = p.Position.Latitude;
                        //var c = ((JArray) f.Geometry.Coordinates).Select(x => (double) x).ToList();
                        //c[0] = p.Position.Longitude;
                        //c[1] = p.Position.Latitude;
                        //f.Geometry.Coordinates = c;
                        var t = api.features.UpdateFeatureAsync(f, layer.Id, f.Id);
                    }
                }
            });
            return p;
        }

        private void UpdateFeature(Feature f, global::DataServer.PoI p, Layer layer)
        {
            p.Data["cs"] = f;


            if (f.Geometry.Type == "Point")
            {
                if (f.Geometry.Coordinates is object[])
                {
                    var co = (object[])f.Geometry.Coordinates;
                    f.Geometry.Coordinates = new JArray(co);
                }
                var c = ((JArray)f.Geometry.Coordinates).Select(x => (double)x).ToList();

                if (p.Position == null || p.Position.Longitude != c[0] || p.Position.Latitude != c[1])
                {
                    p.Position = new Position(c[0], c[1]);
                }
            }
            if (layer.DefaultFeatureType != null)
            {
                p.PoiTypeId = layer.DefaultFeatureType;
            }
            var t = this.PoITypes.FirstOrDefault((pt) => p.PoiTypeId == pt.PoiId);

            //var type = AppState.f
            //p.PoiTypeId = 
            f.Properties.ForEach((v) =>
            {
                p.Labels[v.Key] = v.Value.ToString();
                var mt = t?.MetaInfo.FirstOrDefault((mi) => mi.Id == v.Key);
                if (mt != null)
                {
                    if (mt.Type == MetaTypes.datetime)
                    {
                        p.TimelineString = this.Layer.ID;
                        DateTime dd;
                        if (DateTime.TryParse(v.Value.ToString(), out dd))
                        {
                            p.Date = dd;
                            this.Events.Add(p);
                        }

                    }
                }
            });
            p.ForceUpdate(true, false);
        }

        public override List<MenuItem> GetMenuItems()
        {
            var r = base.GetMenuItems();
            if (!Layer.IsStarted) return r;
            //var setcolor = MenuHelpers.CreateMenuItem("Set Color", MenuHelpers.ColorIcon);
            //setcolor.Click += (e, f) => SelectColor();
            //r.Add(setcolor);
            return r;
        }

        /// <summary>
        ///     Open the file synchronously.
        /// </summary>
        /// <param name="saveDsd">Whether to save a data description file (dsd) too.</param>
        /// <returns>The exception that happened if opening the file did NOT work; null otherwise.</returns>
        public Exception OpenFileSync(bool saveDsd = true)
        {
            try
            {
                Reset();

                PoIs.StartBatch();
                //var isLoaded = LoadOrCreateDataServiceDescription(saveDsd);
                IsLoading = true;

                //MessageBox.Show(this.Name);

                //IsLoading = false;

                //ContentLoaded = true;

                //Execute.OnUIThread(() => Layer.IsLoading = false);

                //Exception processFile = ProcessFile();
                //if (processFile != null)
                //{
                //    Logger.Log("Exception", "ExtendedPoiService", processFile.Message, Logger.Level.Error);
                //    return processFile;
                //}

                //PoIs.FinishBatch();
                //IsLoading = false;

                //ContentLoaded = true;

                //Execute.OnUIThread(() => Layer.IsLoading = false);


                //if (isLoaded || !saveDsd) return null;

                return null; // Nothing went wrong.
            }
            catch (Exception e)
            {
                return e;
            }
        }

        protected virtual void OnFileOpenedEvent(FileOpenedEventArgs e)
        {
            if (FileOpenedEvent != null)
            {
                FileOpenedEvent(this, e);
            }
        }

        /// <summary>
        ///     Process the file. Returns an exception iff this did not work, and null otherwise. Perhaps a little ugly.
        /// </summary>
        /// <returns>An exception if this did not work.</returns>
        protected virtual Exception ProcessFile()
        {
            return null;
        }

        public class FileOpenedEventArgs : EventArgs
        {
            public ExtendedPoiService OpenerService { get; set; }
            public Exception Exception { get; set; }
        }

    }
}