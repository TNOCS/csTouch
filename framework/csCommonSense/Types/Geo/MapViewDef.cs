using Caliburn.Micro;
using csCommon.Types;
using csShared.Geo.Content;
using csShared.Geo.Esri;
using csShared.Utils;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using Newtonsoft.Json.Linq;
using SharpMap.Geometries;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using Point = SharpMap.Geometries.Point;

namespace csShared.Geo
{

    public class GeoPointerArgs : EventArgs
    {
        public MapPoint Position { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class BaseLayersCollection : ObservableCollection<ITileImageProvider>
    {
        public double Version { get; set; }


        private static string GetProperty(JObject obj, string name, string defaultValue)
        {
            JToken jToken;
            string value = defaultValue;
            if (obj.TryGetValue(name, out jToken))
            {
                value = jToken.Value<string>();
            }
            return value;
        }

        public static BaseLayersCollection Parse(string json)
        {
            try
            {
                var layers = JObject.Parse(json);

                var result = new BaseLayersCollection();
                result.Version = layers["version"].Value<double>();

                var ll = layers["layers"];
                foreach (var l in ll.Values<JObject>())
                {

                    // user agent
                    string title = l["Title"].Value<string>();
                    string weburl = l["WebUrl"].Value<string>();

                    string cachelocation = title + "." + Math.Abs(weburl.GetHashCode());
                    string type = GetProperty(l, "Type", "default");
                    string userAgent = GetProperty(l, "UserAgent", "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7");
                    string refer = GetProperty(l, "Refer", "http://maps.google.com");
                    string category = GetProperty(l, "Category", "default");
                    bool cache = bool.Parse(GetProperty(l, "Cache", "true"));
                    switch (type)
                    {
                        case "Bing":
                            var btp = new BingTileProvider(title, cachelocation,
                                weburl, userAgent, refer, l["MapType"].Value<string>(), "1", "");
                            result.Add(btp);
                            break;
                        default:
                            var wtp = new WebTileProvider(title, cachelocation, weburl, userAgent, refer, "");
                            result.Add(wtp);
                            break;
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                Logger.Log("MapViewDef", "Error loading maplayers.json", e.Message, Logger.Level.Error, true);
                return new BaseLayersCollection();
            }

        }
    }


    public class MapViewDef : PropertyChangedBase
    {
        #region fields

        private bool canMove = true;
        private Point center = new Point(0, 0);
        private BoundingBox extent;
        private double height;

        //private Point mousePosition;
        private double opacity = 1;
        private double resolution;
        private double rotation;
        // private MapTypes _selectedMap;
        private bool touchEnabled;
        private bool visible = true;
        private double width;
        private BoundingBox worldExtent;


        public int NearestLevel { get; set; }

        #endregion

        #region events

        //public event EventHandler<MapManipulationStartedEventArgs> MapManipulationStarted;
        public event EventHandler MapManipulationDelta;
        public event EventHandler MapManipulationCompleted;

        //public event EventHandler RedrawRequested;
        public event EventHandler<VisibleChangedEventArgs> VisibleChanged;
        public event EventHandler TransitionStarted;

        #endregion

        private static readonly WebMercator Mercator = new WebMercator();
        private GroupLayer acceleratedlayers;
        private ContentCollection content = new ContentCollection();
        private GroupLayer layers;

        private GroupLayer mapToolsLayer;
        //private Layer accbaseLayer;
        private Layer baseLayer;
        private bool nextEffect;
        public event EventHandler<GeoPointerArgs> GeoPointerAdded;

        public Dictionary<string, string> FolderIcons { get; set; }


        public MapViewDef()
        {

            FolderIcons = new Dictionary<string, string>();
        }

        public bool NextEffect
        {
            get { return nextEffect; }
            set
            {
                nextEffect = value;
                NotifyOfPropertyChange(() => NextEffect);
            }
        }

        public void AddGeoPointer(GeoPointerArgs a)
        {
            var handler = GeoPointerAdded;
            if (handler != null) handler(this, a);
        }

        /// <summary>
        /// Center to the location, highlight the location, keeping the same map resolution.
        /// </summary>
        public void CenterMapOnWgs84Point(DataServer.Position pCenterPosition)
        {
            if (pCenterPosition == null) return;
            PanAndPoint(new System.Windows.Point(pCenterPosition.Longitude, pCenterPosition.Latitude), true);
        }
        /// <summary>
        /// Center to the location, highlight the location, keeping the same map resolution.
        /// </summary>
        /// <param name="p">Location to pan to</param>
        /// <param name="geographic">If true (default), convert the coordinate from lat/lon to web mercator.</param>
        public void PanAndPoint(System.Windows.Point p, bool geographic = true)
        {
            ZoomAndPoint(p, geographic, false);
        }

        /// <summary>
        /// Center to the location, highlight the location, and optionally zoom in.
        /// </summary>
        /// <param name="p">Location to pan to</param>
        /// <param name="geographic">If true (default), convert the coordinate from lat/lon to web mercator.</param>
        /// <param name="zoomIn">If true (default), zoom in too. Otherwise, keep the current resolution.</param>
        public void ZoomAndPoint(System.Windows.Point p, bool geographic = true, bool zoomIn = true)
        {
            Execute.OnUIThread(() =>
            {
                var pos = new MapPoint(p.X, p.Y);
                if (geographic)
                {
                    pos = Mercator.FromGeographic(new MapPoint(p.X, p.Y)) as MapPoint;
                    if (pos == null) return;
                }
                AppStateSettings.Instance.ViewDef.MapControl.ZoomDuration = new TimeSpan(0, 0, 0, 1);
                var zoom = zoomIn ? 500 : extent.Height / 2;
                var env = new Envelope(pos.X - zoom, pos.Y - zoom, pos.X + zoom, pos.Y + zoom);
                AppState.ViewDef.MapControl.Extent = env;
                AppState.ViewDef.AddGeoPointer(new GeoPointerArgs { Position = pos, Duration = TimeSpan.FromSeconds(2) });
            });
        }



        public bool RememberLastPosition { get; set; }

        private GroupLayer esriLayers;
        public GroupLayer EsriLayers
        {
            get { return esriLayers; }
            set
            {
                esriLayers = value;
                NotifyOfPropertyChange(() => EsriLayers);
            }
        }

        public Layer BaseLayer
        {
            get { return baseLayer; }
            set
            {
                baseLayer = value;
                NotifyOfPropertyChange(() => BaseLayer);
            }
        }

        private GroupLayer baseLayers;

        public GroupLayer BaseLayers
        {
            get { return baseLayers; }
            set { baseLayers = value; NotifyOfPropertyChange(() => BaseLayers); }
        }


        public GroupLayer Layers
        {
            get { return layers; }
            set
            {
                layers = value;
                NotifyOfPropertyChange(() => Layers);
            }
        }

        public GroupLayer AcceleratedLayers
        {
            get { return acceleratedlayers; }
            set
            {
                acceleratedlayers = value;
                NotifyOfPropertyChange(() => AcceleratedLayers);
            }
        }

        public GroupLayer MapToolsLayer
        {
            get { return mapToolsLayer; }
            set
            {
                mapToolsLayer = value;
                NotifyOfPropertyChange(() => MapToolsLayer);
            }
        }

        public LayerCollection RootLayer { get; set; }


        public ContentCollection Content
        {
            get { return content; }
            set
            {
                content = value;
                NotifyOfPropertyChange(() => Content);
            }
        }

        private static AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public Map MapControl { get; set; }

        public void GetAllSubLayers(GroupLayer gl, ref List<GraphicsLayer> result)
        {
            foreach (Layer a in gl.ChildLayers)
            {
                if (a is GroupLayer)
                {
                    GetAllSubLayers(a as GroupLayer, ref result);
                }
                if (a is GraphicsLayer) result.Add(a as GraphicsLayer);
            }
        }

        public List<GraphicsLayer> GetAllSubLayers(GroupLayer gl)
        {
            var result = new List<GraphicsLayer>();
            GetAllSubLayers(gl, ref result);
            return result;
        }

        public void GetAllSubGroupLayers(GroupLayer gl, ref List<GroupLayer> result)
        {
            foreach (Layer a in gl.ChildLayers)
            {
                if (a is GroupLayer)
                {
                    result.Add(a as GroupLayer);
                    GetAllSubGroupLayers(a as GroupLayer, ref result);
                }
            }
        }

        public List<GroupLayer> GetAllSubGroupLayers(GroupLayer gl)
        {
            var result = new List<GroupLayer>();
            GetAllSubGroupLayers(gl, ref result);
            return result;

        }

        public GraphicsLayer FindOrCreateGraphicsLayer(string folder, string name)
        {
            return FindOrCreateGraphicsLayer(folder, name, AppStateSettings.Instance.ViewDef.Layers);
        }

        public GraphicsLayer FindOrCreateGraphicsLayer(string folder, string name, GroupLayer baselayer)
        {
            GroupLayer gl = baselayer;
            GroupLayer bl = FindOrCreateGroupLayer(folder, gl);
            if (bl == null) return null;
            Layer r =
              bl.ChildLayers.FirstOrDefault(
                k => k is GraphicsLayer && k.ID != null && k.ID.ToLower() == name.ToLower());
            if (r != null) return (GraphicsLayer)r;
            var rl = new GraphicsLayer { ID = name };
            bl.ChildLayers.Add(rl);
            rl.Initialize();

            return rl;
        }

        public GraphicsLayer FindOrCreateAcceleratedGraphicsLayer(string folder, string name, GroupLayer baselayer,
                                     bool hidden = false)
        {
            GroupLayer gl = baselayer;
            GroupLayer bl = FindOrCreateGroupLayer(folder, gl);
            if (bl == null) return null;
            Layer r =
              bl.ChildLayers.FirstOrDefault(
                k => k is GraphicsLayer && k.ID != null && k.ID.ToLower() == name.ToLower());
            if (r != null) return (GraphicsLayer)r;

            var rl = new GraphicsLayer { ID = name };
            bl.ChildLayers.Add(rl);
            rl.Initialize();
            return rl;
        }

        public void RemoveLayer(string folder)
        {
            GroupLayer gl = AppStateSettings.Instance.ViewDef.Layers;
            List<string> ff = folder.Split('/').ToList();
            if (ff.Count > 1)
            {
                foreach (string f in ff.Take(ff.Count - 1))
                {
                    if (string.IsNullOrEmpty(f) || gl == null) continue;
                    var g =
                      gl.ChildLayers.FirstOrDefault(k => k.ID != null && k.ID.ToLower() == f.ToLower()) as GroupLayer;
                    gl = g;
                }
            }
            if (gl != null)
            {
                Layer t = gl.ChildLayers.FirstOrDefault(k => k.ID == ff.Last());
                if (t != null) gl.ChildLayers.Remove(t);
            }
        }

        public GroupLayer FindOrCreateGroupLayer(string folder)
        {
            return FindOrCreateGroupLayer(folder, AppStateSettings.Instance.ViewDef.Layers);
        }

        public GroupLayer FindOrCreateGroupLayer(string folder, GroupLayer baselayer)
        {
            if (string.IsNullOrEmpty(folder)) return baselayer;
            GroupLayer gl = baselayer;
            if (gl == null) return null;
            List<string> ff = folder.Split('/').ToList();

            foreach (var f in ff)
            {
                if (string.IsNullOrEmpty(f)) continue;
                var g = gl.ChildLayers.FirstOrDefault(k => k.ID != null && String.Equals(k.ID, f, StringComparison.CurrentCultureIgnoreCase)) as GroupLayer;
                if (g == null)
                {
                    g = new GroupLayer { ID = f, ChildLayers = new LayerCollection() };
                    gl.ChildLayers.Add(g);
                    g.Initialize();
                }
                gl = g;
            }
            return gl;
        }

        public GroupLayer FindOrCreateAcceleratedGroupLayer(string folder)
        {
            return FindOrCreateGroupLayer(folder, AppStateSettings.Instance.ViewDef.AcceleratedLayers);
        }


        //public GroupLayer MapTools = new GroupLayer() { ID = "MapTools" };


        public void StartTransition()
        {
            if (TransitionStarted != null) TransitionStarted(this, null);
        }

        public async void CheckOnlineBaseLayerProviders()
        {
            var online = AppState.Config.Get("Map.OnlineBaseStyleDefinitions", "");
            if (!string.IsNullOrEmpty(online))
            {
                WebClient wc = new WebClient();
                var onlinemaps = await wc.DownloadStringTaskAsync(online);
                var ro = BaseLayersCollection.Parse(onlinemaps);

                if (ro != null && ro.Version > BaseLayerProviders.Version)
                {
                    Execute.OnUIThread(() =>
                    {
                        BaseLayerProviders = ro;
                        AppState.TriggerNotification("Base Styles updated");
                        File.WriteAllText("maplayers.json", onlinemaps);
                    });
                }
            }
        }

        /// <summary>
        /// Configure possible base layers
        /// </summary>
        public void InitBaseLayerProviders() // REVIEW TODO fix: async removed
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject())) return;

            var ml = File.ReadAllText("maplayers.json");
            var r = BaseLayersCollection.Parse(ml);

            BaseLayerProviders = r;

            //var BaseLayerProviders = new ObservableCollection<ITileImageProvider>
            //   {
            //     new WebTileProvider("Google Maps", "GoogleMaps",
            //               "http://mt{0}.google.com/vt/lyrs=m@184188536&hl=nl&src=app&x={2}&y={1}&z={3}&s=Galileo",
            //               "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //               "http://maps.google.com",
            //               "http://mt1.google.com/vt/lyrs=m@171000000&hl=nl&src=app&x=33963&s=&y=21291&z=16&s=Gali"),
            //     new WebTileProvider("Google Satellite", "GoogleSatellite",
            //               "https://khms{0}.google.com/kh/v=145&x={2}&src=app&y={1}&z={3}&s=G",
            //               "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //               "http://maps.google.com",
            //               "https://khms0.google.nl/kh/v=145&src=app&x=16&y=10&z=5&s=Gali"),
            //     new BingTileProvider("Bing Roads", "bingroads",
            //               "http://ecn.t{0}.tiles.virtualearth.net/tiles/{1}{2}?g=863&mkt=en-us&lbl=l1&stl=h&shading=hill&n=z",
            //               "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //               @"http://www.bing.com/maps", "r", "1", "http://ak.dynamic.t1.tiles.virtualearth.net/comp/ch/120301?mkt=en-us&it=G,VE,BX,L,LA&shading=hill&og=28&n=z"),
            //     new BingTileProvider("Bing Aerial", "bingaerial",
            //               "http://ecn.t{0}.tiles.virtualearth.net/tiles/{1}{2}?g=863&mkt=en-us&lbl=l1&stl=h&shading=hill&n=z",
            //               "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //               @"http://www.bing.com/maps", "a", "1", "http://ak.dynamic.t1.tiles.virtualearth.net/comp/ch/120301?mkt=en-us&it=A,G,L,LA&shading=hill&og=28&n=z"),
            //     //new WebTileProvider("Google Terrain", "GoogleTerrain",
            //     //          "http://mt{0}.google.com/vt/lyrs=t@128,r@169000000&hl=nl&x={2}&y={1}&z={3}&s=Gal",
            //     //          "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //     //          "http://maps.google.com",
            //     //          "http://khm0.google.nl/kh/v=104&src=app&x=16&y=10&z=5&s=Ga"),
            //     new WebTileProvider("Google Gray", "GoogleGray",
            //               "http://mt{0}.googleapis.com/vt?lyrs=m@175000000&src=apiv3&hl=nl&x={2}&y={1}&z={3}&apistyle=p.s%3A-97&s=Gali&style=api|smartmaps",
            //               "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //               "http://maps.google.com",
            //               "http://khm0.google.nl/kh/v=104&src=app&x=16&y=10&z=5&s=Ga"),
            //     new WebTileProvider("Google Light Gray", "GoogleLightGray",
            //               "http://mt{0}.googleapis.com/vt?lyrs=m@175000000&src=apiv3&hl=nl&x={2}&y={1}&z={3}&apistyle=p.s%3A-97%7Cp.l%3A65&s=Gal&style=api%7Csmartmaps",
            //               "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //               "http://maps.google.com",
            //               "http://khm0.google.nl/kh/v=104&src=app&x=16&y=10&z=5&s=Ga"),
            //     new WebTileProvider("Google Dark Gray", "GoogleDarkGray",
            //               "http://mt{0}.googleapis.com/vt?lyrs=m@175000000&src=apiv3&hl=nl&x={2}&y={1}&z={3}&apistyle=p.s%3A-97%7Cp.l%3A-62&s=Galileo&style=api%7Csmartmaps",
            //               "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //               "http://maps.google.com",
            //               "http://khm0.google.nl/kh/v=104&src=app&x=16&y=10&z=5&s=Ga"),
            //     new WebTileProvider("MapQuest", "mapquestosm",
            //               "http://otile{0}.mqcdn.com/tiles/1.0.0/osm/{3}/{2}/{1}.png",
            //               "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //               "http://maps.google.com",
            //               "http://mtile02.mqcdn.com/tiles/1.0.0/vx/map/13/4228/2689.png")
            //       {DomainStart = 1},
            //     new WebTileProvider("Skobbler", "skobbler",
            //               @"http://tiles{0}.skobbler.net/osm_tiles2/{3}/{2}/{1}.png",
            //               "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //               "maps.skobbler.com", ""),
            //     new WebTileProvider("Open Cycle Map", "opencyclemap",
            //               @"http://c.tile.opencyclemap.org/cycle/{3}/{2}/{1}.png",
            //               "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //               "maps.skobbler.com", ""),

            //     new WebTileProvider("Open Street Map", "osmoriginal",
            //               "http://a.tile.openstreetmap.org/{3}/{2}/{1}.png",
            //               "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //               "http://maps.google.com",
            //               "http://khm0.google.nl/kh/v=104&src=app&x=16&y=10&z=5&s=Ga")
            //       {DomainStart = 1},
            //     new WebTileProvider("Google Traffic", "GoogleTraffic",
            //               "http://mt{0}.google.com/vt?hl=nl&lyrs=m@169000000,traffic|seconds_into_week:-1&x={2}&y={1}&z={3}&style=6",
            //               "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //               "http://maps.google.com",
            //               "http://khm0.google.nl/kh/v=104&src=app&x=16&y=10&z=5&s=Ga")
            //       {
            //         CacheTimeout = new TimeSpan(0, 0, 1, 0)
            //       },
            //       new WebTileProvider("Marine Traffic","MarineTraffic",
            //           "http://tiles.marinetraffic.com/ais/density_tiles/{3}/{2}/tile_{3}_{2}_{1}.png",
            //           "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //               "http://www.marinetraffic.com","http://tiles.marinetraffic.com/ais/density_tiles/9/260/tile_9_260_168.png"),
            //       new WebTileProvider("Wind Vector","WindVector",
            //           "http://www.openportguide.org/tiles/actual/wind_vector/5/{3}/{2}/{1}.png",
            //           "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //               "http://www.openportguide.org/","")
            //               {
            //         CacheTimeout = new TimeSpan(0, 0, 1, 0)
            //       },

            //       new WebTileProvider("Surface Pressure","surfacepressure",
            //           "http://www.openportguide.org/tiles/actual/surface_pressure/5/{3}/{2}/{1}.png",
            //           "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //               "http://www.openportguide.org/","")
            //               {
            //         CacheTimeout = new TimeSpan(0, 0, 1, 0)
            //       },
            //       new WebTileProvider("Precipitation","precipitation",
            //           "http://www.openportguide.org/tiles/actual/precipitation/5/{3}/{2}/{1}.png",
            //           "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //               "http://www.openportguide.org/","")
            //               {
            //         CacheTimeout = new TimeSpan(0, 0, 1, 0)
            //       },


            //     new WebTileProvider("Water Color", "watercolor",
            //               "http://c.tile.stamen.com/watercolor/{2}/{2}/{1}.jpg",
            //               "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //               "http://maps.stamen.com",
            //               "http://c.tile.stamen.com/watercolor/12/2122/1329.jpg")
            //       {DomainStart = 1},
            //     new WebTileProvider("Toner", "toner", "http://d.tile.stamen.com/toner/{3}/{2}/{1}.png",
            //               "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //               "http://maps.stamen.com",
            //               "http://c.tile.stamen.com/watercolor/12/2122/1329.jpg")
            //       {DomainStart = 1},
            //     //new WebTileProvider("Night", "night",
            //     //          "http://a.tiles.mapbox.com/v3/examples.map-cnkhv76j/{3}/{2}/{1}.png",
            //     //          "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //     //          "http://mapbox.com",
            //     //          "http://a.tiles.mapbox.com/v3/examples.map-cnkhv76j/12/2122/1329.jpg")
            //     //  {DomainStart = 1},
            //     //new WebTileProvider("Mapbox Streets", "mapboxstreets",
            //     //          "http://b.tiles.mapbox.com/v3/mapbox.mapbox-streets/{3}/{2}/{1}.png",
            //     //          "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //     //          "http://mapbox.com",
            //     //          "http://b.tiles.mapbox.com/v3/mapbox.mapbox-streets/17/67929/42572.png")
            //     //  {DomainStart = 1},
            //       new WebTileProvider("Mapbox Old Sea Map", "mapboxoldseamap",
            //               "http://c.tiles.mapbox.com/v3/examples.a3cad6da/{3}/{2}/{1}.png",
            //               "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //               "http://mapbox.com",
            //               "http://c.tiles.mapbox.com/v3/examples.a3cad6da/12/2101/1345.png")
            //       {DomainStart = 1},
            //       new WebTileProvider("Mapbox Comic", "mapboxcomic",
            //               "http://c.tiles.mapbox.com/v3/examples.bc17bb2a/{3}/{2}/{1}.png",
            //               "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //               "http://mapbox.com",
            //               "http://c.tiles.mapbox.com/v3/examples.bc17bb2a/12/2101/1345.png")
            //       {DomainStart = 1},
            //       //
            //       //http://geodata.nationaalgeoregister.nl/tiles/service/wmts/ahn1?SERVICE=WMTS&REQUEST=GetTile&VERSION=1.0.0&LAYER=ahn2_05m_ruw&STYLE=_null&TILEMATRIXSET=EPSG%3A28992&TILEMATRIX=EPSG%3A28992%3A13&TILEROW=3005&TILECOL=4813&FORMAT=image%2Fpng8
            //       //


            //       new WebTileProvider("ahn2", "ahn2",
            //               "http://geodata.nationaalgeoregister.nl/tiles/service/wmts/ahn1?SERVICE=WMTS&REQUEST=GetTile&VERSION=1.0.0&LAYER=ahn2_05m_ruw&STYLE=_null&TILEMATRIXSET=EPSG%3A28992&TILEMATRIX=EPSG%3A28992%3A{3}&TILEROW={1}&TILECOL={2}&FORMAT=image%2Fpng8",
            //               "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //               "http://ahn.nl",
            //               "http://c.tiles.mapbox.com/v3/examples.bc17bb2a/12/2101/1345.png")
            //       {DomainStart = 1},
            //       //new WebTileProvider("Mapbox Light Blue", "mapboxlightblue",
            //     //          "http://a.tiles.mapbox.com/v3/mapbox.mapbox-osgoode/{3}/{2}/{1}.png",
            //     //          "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //     //          "http://mapbox.com",
            //     //          "http://a.tiles.mapbox.com/v3/mapbox.mapbox-osgoode/17/67929/42572.png")
            //     //  {DomainStart = 1},
            //     //new WebTileProvider("Mapbox Blue Gray", "mapboxbluegray",
            //     //          "http://b.tiles.mapbox.com/v3/examples.map-a1dcgmtr/{3}/{2}/{1}.png",
            //     //          "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //     //          "http://mapbox.com",
            //     //          "http://b.tiles.mapbox.com/v3/examples.map-a1dcgmtr/7/65/42.png")
            //     //  {DomainStart = 1},
            //     //new WebTileProvider("Mapbox Terrain", "mapboxterrain",
            //     //          "http://a.tiles.mapbox.com/v3/examples.map-d40qac29/{3}/{2}/{1}.png",
            //     //          "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //     //          "http://mapbox.com",
            //     //          "http://a.tiles.mapbox.com/v3/examples.map-d40qac29/11/392/769.png")
            //     //  {DomainStart = 1},
            //     CreateOSMProvider(1, "Cloud Made", "cloudmade1",
            //              "http://khm0.google.nl/kh/v=104&src=app&x=16&y=10&z=5&s=Ga"),
            //     CreateOSMProvider(8, "Red Alert", "redalert",
            //              "http://khm0.google.nl/kh/v=104&src=app&x=16&y=10&z=5&s=Ga"),
            //     CreateOSMProvider(22688, "Water vs Land", "watervsland",
            //              "http://khm0.google.nl/kh/v=104&src=app&x=16&y=10&z=5&s=Ga"),
            //     CreateOSMProvider(999, "Midnight", "Midnight",
            //              "http://khm0.google.nl/kh/v=104&src=app&x=16&y=10&z=5&s=Ga"),
            //     CreateOSMProvider(997, "Fresh", "Fresh",
            //              "http://khm0.google.nl/kh/v=104&src=app&x=16&y=10&z=5&s=Ga"),
            //     CreateOSMProvider(2402, "Clean", "Clean",
            //              "http://khm0.google.nl/kh/v=104&src=app&x=16&y=10&z=5&s=Ga")
            //   };
            ////https://mts0.google.com/vt/lyrs=m@184188536&hl=en&src=app&x=2122&y=1329&z=12&s=Galileo

            ////https://khms0.google.com/kh/v=111&src=app&x=33958&s=&y=21282&z=16&s=Gali


            //BaseLayers.Add(new WebTileProvider("Ninentendo", "Nintendo",
            //  "http://mt{0}.google.com/vt/lyrs=8bit,m@174000000&hl=nl&x={2}&y={1}&z={3}&s=Gal",
            //               "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //               "http://maps.google.com", "http://khm0.google.nl/kh/v=104&src=app&x=16&y=10&z=5&s=Ga"));


            //BaseLayers.Add(new WebTileProvider("Geluidsbelasting", "geluidsbelasting", "http://www.rijkswaterstaat.nl/apps/geoservices/rwsnl/tilecache/geluid_lden_topo2/{3}/000/000/{1}/000/000/{2}.jpeg",
            //  "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7", "http://www.rijkswaterstaat.nl", "http://www.rijkswaterstaat.nl/apps/geoservices/rwsnl/tilecache/geluid_lden_topo2/07/000/000/152/000/000/162.jpeg"));

            //BaseLayers.Add(new WebTileProvider("Yahoo Maps","yahoomaps",
            //  @"http://{0}.maptile.lbs.ovi.com/maptiler/v2/maptile/279af375be/normal.day/{3}/{2}/{1}/256/png8?lg=ENG&token=TrLJuXVK62IQk0vuXFzaig%3D%3D&requestid=yahoo.prod&app_id=eAdkWGYRoc4RfxVo0Z4B",
            //  "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //  "http://maps.yahoo.com", "http://4.maptile.lbs.ovi.com/maptiler/v2/maptile/279af375be/normal.day/8/60/99/256/png8?lg=ENG&token=TrLJuXVK62IQk0vuXFzaig%3D%3D&requestid=yahoo.prod&app_id=eAdkWGYRoc4RfxVo0Z4B") { DomainStart = 1 });

            //BaseLayers.Add(new WebTileProvider("Nokia Terrain", "nokiaterrain",
            //                  @"http://{0}.maptile.lbs.ovi.com/maptiler/v2/maptile/279af375be/terrain.day/{3}/{2}/{1}/256/png8?token=fee2f2a877fd4a429f17207a57658582&appId=nokiaMaps",
            //                  "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
            //                  "maps.nokia.com", "http://khm0.google.nl/kh/v=104&src=app&x=16&y=10&z=5&s=Ga") { DomainStart = 1 });

            //http://ecn.dynamic.t2.tiles.virtualearth.net/comp/CompositionHandler/1202?mkt=en-us&it=G,VE,BX,L,LA&shading=hill&n=z


            //

            //SelectedBaseLayer = BaseLayerProviders[1];
        }

        public void ChangeMapType(MapType mapType)
        {

            string t = mapType.ToString().Replace("_", " ");
            ChangeMapType(t);
            SelectedMapType = t;
        }

        private string selectedMapType;

        public string SelectedMapType
        {
            get { return selectedMapType; }
            set { selectedMapType = value; NotifyOfPropertyChange(() => SelectedMapType); }
        }

        private void UpdateMapActivations()
        {
            foreach (var p in BaseLayerProviders) p.Activated = false;
            foreach (WebTileLayer l in BaseLayers.Where(k => ((WebTileLayer)k).TileProvider != null)) l.TileProvider.Activated = true;
        }

        public void RemoveMapType(string mapType)
        {
            Execute.OnUIThread(() =>
            {
                try
                {
                    var map = AppState.ViewDef.BaseLayerProviders.FirstOrDefault(bl => bl.Title.ToLower() == mapType.ToLower());
                    if (map != null)
                    {
                        var l =
                            AppState.ViewDef.BaseLayers.ChildLayers.FirstOrDefault(
                                k => ((WebTileLayer)k).TileProvider == map);
                        if (l != null)
                        {
                            AppState.ViewDef.BaseLayers.ChildLayers.Remove(l);
                        }

                        AppState.TriggerScriptCommand(this, ScriptCommands.UpdateLayers);
                    }
                }
                catch (Exception e)
                {
                    Logger.Log("Map Selection", "Error changing maptype", e.Message, Logger.Level.Error);
                }
            });
            UpdateMapActivations();
        }

        public void AddMBTileLayer(string file)
        {
            var f = new FileInfo(file);
            var wtp = new WebTileProvider();
            wtp.MBTileFile = file;
            wtp.PreviewImage = Path.ChangeExtension(f.FullName, ".png");
            wtp.Title = f.Name.ToLower().Replace(".mbtiles", "");
            AppState.ViewDef.BaseLayerProviders.Insert(0, wtp);

        }

        public void AddMapType(string mapType, double layerOpacity = 1)
        {
            Execute.OnUIThread(() =>
            {
                try
                {
                    var map = BaseLayerProviders.FirstOrDefault(bl => string.Equals(bl.Title, mapType, StringComparison.CurrentCultureIgnoreCase));
                    if (map == null) return;
                    var wtl = new WebTileLayer { TileProvider = map, ID = mapType };
                    BaseLayers.ChildLayers.Add(wtl);
                    wtl.Opacity = layerOpacity;
                    wtl.Initialize();
                    AppState.TriggerScriptCommand(this, ScriptCommands.UpdateLayers);
                }
                catch (Exception e)
                {
                    Logger.Log("Map Selection", "Error changing maptype", e.Message, Logger.Level.Error);
                }
            });
            UpdateMapActivations();
        }

        public void ChangeMapType(string mapType)
        {
            Execute.OnUIThread(() =>
            {
                try
                {

                    var existing = BaseLayers.ChildLayers.FirstOrDefault(k => ((WebTileLayer)k).TileProvider != null && ((WebTileLayer)k).TileProvider.Title == mapType);
                    if (existing != null)
                    {
                        BaseLayers.ChildLayers.Move(BaseLayers.ChildLayers.IndexOf(existing), BaseLayers.ChildLayers.Count - 1);
                        existing.Visible = true;
                        existing.Opacity = 1.0;

                    }
                    else
                    {
                        var map = AppStateSettings.Instance.ViewDef.BaseLayerProviders.FirstOrDefault(bl => bl.Title.ToLower() == mapType.ToLower());
                        if (map == null) return;
                        SelectedBaseLayer = map;
                        //var l = AppState.ViewDef.BaseLayers.ChildLayers.LastOrDefault();
                        //if (l is WebTileLayer)
                        //{
                        //    ((WebTileLayer) l).TileProvider = map;
                        //}
                        if (BaseLayers.ChildLayers.Any())
                        {
                            var gbl = BaseLayers.ChildLayers.Last();
                            var layer = gbl as WebTileLayer;
                            if (layer == null) return;
                            layer.ID = SelectedBaseLayer.Title;
                            layer.TileProvider = SelectedBaseLayer;
                            layer.Refresh();
                            SelectedBaseLayer = map;
                            SelectedMapType = mapType;
                            AppState.TriggerScriptCommand(this, ScriptCommands.UpdateLayers);
                        }
                        else
                        {
                            AddMapType(mapType);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Log("Map Selection", "Error changing maptype", e.Message, Logger.Level.Error);
                }
            });
            UpdateMapActivations();
        }

        private static WebTileProvider CreateOSMProvider(int id, string title, string folder, string previewImage)
        {
            return new WebTileProvider(title, folder,
                "http://b.tile.cloudmade.com/8ee2a50541944fb9bcedded5165f09d9/" + id +
                "/256/{3}/{2}/{1}.png",
                "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7",
                "http://cloudmade.com", previewImage);
        }

        public static void ZoomTo(Layer l)
        {
            Execute.OnUIThread(() =>
            {
                var e = new Envelope();
                ZoomTo(l, ref e);
                if (e.Extent != null) AppState.ViewDef.MapControl.Extent = e;
            });
        }

        public static void ZoomTo(Layer l, ref Envelope e)
        {
            var layer = l as GroupLayer;
            if (layer != null)
            {
                foreach (var ll in layer.ChildLayers)
                {
                    ZoomTo(ll, ref e);
                }
            }
            else
            {
                var graphicsLayer = l as GraphicsLayer;
                if (graphicsLayer != null)
                {
                    foreach (var g in graphicsLayer.Graphics)
                    {
                        ZoomTo(g, ref e);
                    }
                }
                else
                {
                    var elementLayer = l as ElementLayer;
                    if (elementLayer == null) return;
                    var elayer = elementLayer;
                    if (elayer.FullExtent != null)
                        e = elayer.FullExtent;

                }
            }
        }

        private static void ZoomTo(Graphic g, ref Envelope e)
        {
            try
            {
                if (g == null || g.Geometry == null || g.Geometry.Extent == null) return;
                if (double.IsNaN(e.XMax))
                {
                    e = g.Geometry.Extent;
                    return;
                }

                var ge = g.Geometry.Extent;
                if (!double.IsNaN(ge.XMax) && ge.XMax > e.XMax) e.XMax = ge.XMax;
                if (!double.IsNaN(ge.YMax) && ge.YMax > e.YMax) e.YMax = ge.YMax;
                if (!double.IsNaN(ge.YMin) && ge.YMin < e.YMin) e.YMin = ge.YMin;
                if (!double.IsNaN(ge.XMin) && ge.XMin < e.XMin) e.XMin = ge.XMin;
            }
            catch (Exception ex)
            {
                Logger.Log("Map Zoom", "Error in ZoomTo operation", ex.Message, Logger.Level.Error);
            }
        }

        public void ZoomTo(KmlPoint kmlPoint, double r)
        {
            if (MapControl == null) return;
            var p = (MapPoint)Mercator.FromGeographic(new MapPoint(kmlPoint.Longitude, kmlPoint.Latitude));
            var env = new Envelope(p.X - (r / 2), p.Y - (r / 2), p.X + (r / 2), p.Y + (r / 2));
            MapControl.ZoomTo(env);
            Resolution = r;
        }

        public void UpdateWorldExtent()
        {
            var bl = (MapPoint)Mercator.ToGeographic(new MapPoint(MapControl.Extent.XMin, MapControl.Extent.YMin));
            var tr = (MapPoint)Mercator.ToGeographic(new MapPoint(MapControl.Extent.XMax, MapControl.Extent.YMax));

            WorldExtent = new BoundingBox(bl.X, bl.Y, tr.X, tr.Y);
        }

        #region Raise MapManipulation Events

        public void RaiseManipulationCompleted()
        {
            if (MapManipulationCompleted != null)
                MapManipulationCompleted(this, null);
        }

        public void RaiseManipulationDelta()
        {
            if (MapManipulationDelta != null)
                MapManipulationDelta(this, null);
        }

        #endregion

        #region properties

        private BaseLayersCollection baseLayerProviders = new BaseLayersCollection();
        private ITileImageProvider selectedBaseLayer;
        private StoredLayerCollection storedLayers = new StoredLayerCollection();

        public StoredLayerCollection StoredLayers
        {
            get { return storedLayers; }
            set
            {
                storedLayers = value;
                NotifyOfPropertyChange(() => StoredLayers);
            }
        }


        public FrameworkElement Map { get; set; }

        public BaseLayersCollection BaseLayerProviders
        {
            get { return baseLayerProviders; }
            set
            {
                baseLayerProviders = value;
                NotifyOfPropertyChange(() => BaseLayerProviders);
            }
        }

        public ITileImageProvider SelectedBaseLayer
        {
            get { return selectedBaseLayer; }
            private set
            {
                if (selectedBaseLayer != value)
                {
                    selectedBaseLayer = value;
                    NotifyOfPropertyChange(() => SelectedBaseLayer);
                    if (SelectedBaseLayerChanged != null)
                        SelectedBaseLayerChanged(this, null);
                }
            }
        }

        public double Rotation
        {
            get { return rotation; }
            set
            {
                rotation = value;
                NotifyOfPropertyChange(() => Rotation);
            }
        }


        public bool CanMove
        {
            get { return canMove; }
            set
            {
                canMove = value;
                NotifyOfPropertyChange(() => CanMove);
            }
        }


        public double Width
        {
            get { return width; }
            set
            {
                width = value;
                NotifyOfPropertyChange(() => Width);
            }
        }

        public double Height
        {
            get { return height; }
            set
            {
                height = value;
                NotifyOfPropertyChange(() => Height);
            }
        }

        public BoundingBox Extent
        {
            get { return extent; }
            set
            {
                extent = value;
                NotifyOfPropertyChange(() => Extent);
            }
        }


        public bool Visible
        {
            get { return visible; }
            set
            {
                visible = value;

                NotifyOfPropertyChange(() => Visible);
                if (VisibleChanged != null) VisibleChanged(this, new VisibleChangedEventArgs { Visible = value });
            }
        }


        public bool TouchEnabled
        {
            get { return touchEnabled; }
            set
            {
                touchEnabled = value;
                NotifyOfPropertyChange(() => TouchEnabled);
            }
        }

        public double Opacity
        {
            get { return opacity; }
            set
            {
                opacity = value;
                NotifyOfPropertyChange(() => Opacity);
            }
        }


        public double Resolution
        {
            get { return resolution; }
            set
            {
                if (resolution == value) return;
                resolution = value;

                NotifyOfPropertyChange(() => Resolution);
            }
        }

        public Point Center
        {
            get { return center; }
            set
            {
                if (center.X == value.X && center.Y == value.Y) return;
                center = value;

                NotifyOfPropertyChange(() => Center);
            }
        }

        public BoundingBox WorldExtent
        {
            get { return worldExtent; }
            private set
            {
                if (Equals(worldExtent, value)) return;
                worldExtent = value;
                resolution = MapControl.Resolution;

                NotifyOfPropertyChange(() => WorldExtent);
            }
        }


        private CoordinateType CoordinateType { get; set; }

        public event EventHandler SelectedBaseLayerChanged;



        #endregion

        #region events

        #endregion

        #region maptype

        #endregion

        #region conversion

        /// <summary>
        /// Returns point on the map for a specific lat/lon 
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public System.Windows.Point MapPoint(KmlPoint p)
        {
            if (MapControl != null)
            {
                System.Windows.Point p1 =
                  MapControl.MapToScreen((MapPoint)Mercator.FromGeographic(new MapPoint(p.Longitude, p.Latitude)));
                return p1;
            }
            if (p == null) return new System.Windows.Point(0, 0);
            System.Windows.Point world = SphericalMercator.FromLonLat(p.Longitude, p.Latitude);
            Point pw = WorldToMap(new Point(world.X, world.Y));
            return new System.Windows.Point(Width - pw.X, Height - pw.Y);
        }

        public System.Windows.Point MapPoint(MapPoint p)
        {
            if (MapControl != null)
            {
                if (p.SpatialReference.WKID == 102100)
                    return MapControl.MapToScreen(p);
                return MapControl.MapToScreen((MapPoint)Mercator.FromGeographic(p));
            }
            return new System.Windows.Point();
        }

        public Point WorldToMap(Point point)
        {
            return WorldToView(point.X, point.Y);
        }

        public Point WorldToView(double x, double y)
        {
            System.Windows.Point world = SphericalMercator.FromLonLat(WorldExtent.MaxY, WorldExtent.MinX);
            return new Point((world.X - x) / Resolution, (y - world.Y) / Resolution);
        }

        public Point ViewToWorld(double x, double y)
        {
            if (MapControl != null)
            {
                MapPoint a = MapControl.ScreenToMap(new System.Windows.Point(x, y));
                var b = (MapPoint)Mercator.ToGeographic(a);

                return new Point(b.Y, b.X);
            }

            // get pos within extent
            double _x = (((Extent.MaxX - Extent.MinX) / Width) * x) + Extent.MinX;
            double _y = (((Extent.MinY - Extent.MaxY) / Height) * y) + Extent.MaxY;

            Point l = CoordinateUtils.ConvertCoordinateFromXY(new Point(_x, _y), CoordinateType);
            return l;
        }

        public System.Windows.Point WorldToMap(double x, double y)
        {
            Point w = WorldToMap(new Point(x, y));
            return new System.Windows.Point(w.X, w.Y);
        }

        public bool IsInView(KmlPoint kmlPoint)
        {
            return WorldExtent.Contains(new Point(kmlPoint.Latitude, kmlPoint.Longitude));
        }

        #endregion

        internal void ClearMapCache()
        {

            MapControl.IsHitTestVisible = false;
            AppState.TriggerNotification("Clearing cache... please wait");
            foreach (WebTileLayer wtl in BaseLayers)
            {
                wtl.CloseCache();
            }
            ThreadPool.QueueUserWorkItem(delegate
            {

                string dir = Path.Combine(AppStateSettings.CacheFolder, "webtiles"); // REVIEW TODO: Used Path instead of String concat.
                if (Directory.Exists(dir))
                {
                    var di = new DirectoryInfo(dir);

                    foreach (FileInfo f in di.GetFiles())
                    {
                        File.Delete(f.FullName);
                    }

                }
                Execute.OnUIThread(() =>
                {
                    AppState.TriggerNotification("Cache cleared");
                    MapControl.IsHitTestVisible = true;
                });
            });
        }

        public void UpdateLayers()
        {
            AppState.TriggerScriptCommand(this, ScriptCommands.UpdateLayers);
        }
    }

    public class VisibleChangedEventArgs : EventArgs
    {
        public bool Visible { get; set; }
    }
}