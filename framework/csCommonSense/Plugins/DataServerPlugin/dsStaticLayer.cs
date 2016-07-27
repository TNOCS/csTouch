using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using Caliburn.Micro;
using csCommon.Types;
using csCommon.Types.Geometries;
using csShared;
using csShared.Controls.Popups.MenuPopup;
using csShared.Geo;
using csShared.Utils;
using DataServer;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using ESRI.ArcGIS.Client.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Action          = System.Action;
using PointCollection = ESRI.ArcGIS.Client.Geometry.PointCollection;
using Polygon         = ESRI.ArcGIS.Client.Geometry.Polygon;
using Task            = System.Threading.Tasks.Task;

namespace csDataServerPlugin
{
    [DebuggerDisplay("Layer {ID}, {PoiLayers.Count} sub layers, {Children.Count} children")]
    public class dsStaticLayer : dsBaseLayer, ILayerWithMoreChildren, IServiceLayer, IStartStopLayer, ITabLayer, IMenuLayer, IOnlineLayer
    {
        private readonly WebMercator mercator = new WebMercator();

        private readonly Dictionary<string, dsStaticSubLayer> subLayers;

        //private bool canStart;
        //private bool isStarted;
        //private string state;

        private bool zoomVisible;

        public dsStaticLayer(PoiService s, DataServerPlugin p)
        {
            if (s == null)
            {
                csCommon.Logging.LogCs.LogMessage(String.Format("dsStaticLayer: Create PoiService '{0}'", "empty"));
                s = new PoiService();
            }
            Service   = s;
            Visible   = false;
            ID        = Service.Name;
            plugin    = p;
            subLayers = new Dictionary<string, dsStaticSubLayer>();
            Children  = new ObservableCollection<Layer>();

            s.AllPoisRefreshed += (delegate { RefreshAllVisiblePois(); });
        }

        public bool ZoomVisible
        {
            get { return zoomVisible; }
            set
            {
                zoomVisible = value;
                OnPropertyChanged("ZoomVisible");
            }
        }

        public Guid Id
        {
            get { return service.Id; }
        }

        public ObservableCollection<Layer> Children { get; set; }

        #region IStartStopLayer Members

        public new void Stop() {
            Execute.OnUIThread(() => {
                base.Stop();
                //isStarted = false;
                CloseTab();

                TriggerStopped();

                foreach (var baseContent in Service.PoIs) {
                    var p = (PoI) baseContent;
                    RemovePoi(p);
                }
                plugin.Dsb.UnSubscribe(Service);
                foreach (var layer in ChildLayers.Where(k => k is GraphicsLayer)) {
                    var c = (GraphicsLayer) layer;
                    c.ClearGraphics();
                }
                subLayers.Clear();
                ChildLayers.Clear();
                Visible = false;
                AppState.ViewDef.UpdateLayers();
            });
        }

        public new void Start(bool share = false)
        {
            var dguid = AppState.AddDownload("Start Layer", "");
            service.CheckOriginalBackup();
            base.Start();
            Init();
            //EV Removed: pois aren't loaded yet. And UpdateAllPois is also called when service is initialized.
            //UpdateAllPois();
            //Service.PoIs.CollectionChanged -= PoisCollectionChanged;
            //Service.PoIs.CollectionChanged += PoisCollectionChanged;
            //Service.PoiUpdated += ServicePoiUpdated;
            Service.Initialized -= ServiceInitialized;
            Service.Initialized += ServiceInitialized;
            //AppState.TimelineManager.FocusTimeUpdated += TimelineManagerFocusTimeChanged;
            //AppState.TimelineManager.TimeContentChanged += TimelineManagerFocusTimeChanged;

            TriggerStarted();
            if (Service.Settings != null && Service.Settings.OpenTab) OpenTab();

            //Task.Run(delegate { Service.ReadSensorFile(); });

            Execute.OnUIThread(() => { Visible = true; });
            //Service.SaveBinarySensorData();
            
            plugin.Dsb.Subscribe(Service);

            AppState.FinishDownload(dguid);
        }

        #endregion

        public void OpenPoiPopup(BaseContent c)
        {
        }

        //public PoiService Service
        //{
        //    get { return service; }
        //    set { service = value; }
        //}

        public override void Initialize()
        {
            base.Initialize();

            Visible = false;
            SpatialReference = new SpatialReference(4326);
        }

        private async void UpdateAllPois()
        {
            Execute.OnUIThread(async () => {
                Service.PoIs.StartBatch();
                foreach (var baseContent in Service.PoIs)
                {
                    var p = (PoI)baseContent;
                    AddPoi(p);
                }
                Service.PoIs.FinishBatch();
                if (Service.PoIs.Any())
                    await Task.Run(() => Service.ReadSensorFile());
            });
        }

        private void ServiceInitialized(object sender, EventArgs e)
        {
            UpdateAllPois();
            Service.PoIs.CollectionChanged -= PoisCollectionChanged;
            Service.PoIs.CollectionChanged += PoisCollectionChanged;
            Service.PoiUpdated             += ServicePoiUpdated;
            Service.PoIs.BatchStarted      -= PoIs_BatchStarted;
            Service.PoIs.BatchStarted      += PoIs_BatchStarted;
            Service.PoIs.BatchFinished     -= PoIs_BatchFinished;
            Service.PoIs.BatchFinished     += PoIs_BatchFinished;

            AppState.ViewDef.MapControl.ExtentChanged -= MapControlExtentChanged;
            AppState.ViewDef.MapControl.ExtentChanged += MapControlExtentChanged;
            Execute.OnUIThread(() => {
                Service.SetExtent(AppState.ViewDef.WorldExtent);
                IsLoading = false;
            });
        }

        void PoIs_BatchFinished(object sender, EventArgs e)
        {
            Execute.OnUIThread(() =>
            {
                var all = batch.Take(batch.Count).ToArray();
                for (var i=0;i<all.Count();i++) AddPoi(all[i]);
            });
        }

        void PoIs_BatchStarted(object sender, EventArgs e)
        {
            batch = new ConcurrentBag<PoI>();
        }

        private void ServicePoiUpdated(object sender, PoiUpdatedEventArgs e)
        {
            var poi = e.Poi as PoI;
            if (poi != null) UpdatePoiStyle(poi);
        }

        private ConcurrentBag<PoI> batch = new ConcurrentBag<PoI>();

        private void PoisCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!Service.IsInitialized) return;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    Dispatcher.BeginInvoke(new Action(delegate { foreach (PoI p in Service.PoIs) RemovePoi(p); }));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Dispatcher.BeginInvoke(new Action(delegate { foreach (PoI p in e.OldItems) RemovePoi(p); }));
                    break;
                case NotifyCollectionChangedAction.Add:
                    foreach (PoI p in e.NewItems)
                    {
                        if (Service.PoIs.IsBatchLoading>0)
                        {
                            batch.Add(p);
                        }
                        else
                        {
                            AddPoi(p);
                        }
                    }
                    break;
            }
        }

        private static void UpdatePoiStyleGraphics(PoI p, bool checkVisibility)
        {
            if (!p.Data.ContainsKey("graphics") || !(p.Data["graphics"] is List<StaticGraphic>)) return;
            var staticGraphics = (List<StaticGraphic>) p.Data["graphics"];
            foreach (var g in staticGraphics)
            {
                if (p.NEffectiveStyle.Visible != null && g.Visible != p.NEffectiveStyle.Visible.Value)
                {
                    g.Visible = p.NEffectiveStyle.Visible.Value;
                }

                if (checkVisibility)
                {
                    g.Visible = true;
                    g.UpdateVisibility();
                }
                var sp2 = (SimpleFillSymbol) g.Symbol;
                if (sp2 == null) return;

                var changed = p.NEffectiveStyle.FillColor.ToString() != sp2.Fill.ToString() || p.NEffectiveStyle.FillOpacity != sp2.Fill.Opacity;
                if (changed)
                {
                    g.Symbol = new SimpleFillSymbol
                    {
                        BorderBrush     = new SolidColorBrush(p.NEffectiveStyle.StrokeColor.Value),
                        BorderThickness = p.NEffectiveStyle.StrokeWidth.Value,
                        Fill            = new SolidColorBrush(p.NEffectiveStyle.FillColor.Value) {Opacity = p.NEffectiveStyle.FillOpacity.Value}
                    };
                }
            }
        }

        public void UpdatePoiStyle(PoI p, bool checkVisibility = false) // REVIEW TODO fix: 'new' keyword removed.
        {
            if (!p.Data.ContainsKey("graphic") || !(p.Data["graphic"] is StaticGraphic))
            {
                UpdatePoiStyleGraphics(p, checkVisibility);
                return;
            }
            var g = (StaticGraphic) p.Data["graphic"];
            if (p.NEffectiveStyle.Visible != null && g.Visible != p.NEffectiveStyle.Visible.Value)
            {
                g.Visible = p.NEffectiveStyle.Visible.Value;
            }

            
            switch (p.NEffectiveStyle.DrawingMode)
            {
                case DrawingModes.Image:
                    GetGraphic(p, ref g);
                    //var gi = g.ImageGraphic;
                    break;
                case DrawingModes.Point:
                    var s = (SimpleMarkerSymbol) g.Symbol;
                    if (s == null) return;
                    /* icon size is controlled in updatescale
                    if (p.NEffectiveStyle.IconWidth != null && p.NEffectiveStyle.IconWidth.Value != s.Size)
                    {
                        s.Size = p.NEffectiveStyle.IconWidth.Value + 5;
                        if (g.ImageGraphic != null)
                        {
                            var ps = g.ImageGraphic.Symbol as PictureMarkerSymbol;
                            if (ps != null)
                            {
                                ps.Width = s.Size - 5;
                                ps.Height = s.Size - 5;

                            }
                        }
                    }*/
                    if (p.NEffectiveStyle.FillColor.ToString() != s.Color.ToString())
                    {
                        if (p.NEffectiveStyle.FillOpacity == null) return;
                        var b = new SolidColorBrush(p.NEffectiveStyle.FillColor.Value) {Opacity = p.NEffectiveStyle.FillOpacity.Value};
                        //if (s.Color != b) 
                        s.Color = b;
                    }
                    if (g.ImageGraphic != null)
                    {
                        var source = ((PictureMarkerSymbol) (g.ImageGraphic.Symbol)).Source;
                        if (source != null && !source.ToString().EndsWith(p.NEffectiveStyle.Icon))
                        {
                            RemovePoi(p);
                            AddPoi(p);
                        }
                    }
                    //g.UpdateScale();

                    break;
                case DrawingModes.Polyline:
                    var sp = (SimpleLineSymbol) g.Symbol;
                    if (sp == null) return;
                    if (p.NEffectiveStyle.StrokeColor.ToString() != sp.Color.ToString())
                    {
                        sp.Color = new SolidColorBrush(p.NEffectiveStyle.StrokeColor.Value)
                            {
                                Opacity = p.NEffectiveStyle.StrokeOpacity.Value
                            };
                    }
                    if (p.NEffectiveStyle.StrokeWidth.Value != sp.Width)
                    {
                        sp.Width = p.NEffectiveStyle.StrokeWidth.Value;
                    }
                    break;
                case DrawingModes.Polygon:
                    
                    var sp2 = g.Symbol as SimpleFillSymbol;
                    if (sp2 == null) return;

                    var changed = p.NEffectiveStyle.FillColor.ToString() != sp2.Fill.ToString() || p.NEffectiveStyle.FillOpacity != sp2.Fill.Opacity;
                    if (changed)
                    {
                        g.Symbol = new SimpleFillSymbol
                            {
                                BorderBrush = new SolidColorBrush(p.NEffectiveStyle.StrokeColor.Value),
                                BorderThickness = p.NEffectiveStyle.StrokeWidth.Value,
                                Fill = new SolidColorBrush(p.NEffectiveStyle.FillColor.Value) {Opacity = p.NEffectiveStyle.FillOpacity.Value}
                            };
                    }
                    break;
            }
            if (checkVisibility)
            {
                //g.Visible = true;
                g.UpdateVisibility();
            }
        }

        private new void RemovePoi(PoI p)
        {
            var l = FindPoiLayer(p);
            if (p.Data.ContainsKey("FloatingElement"))
            {
                var fe = (FloatingElement)p.Data["FloatingElement"];
                if (fe != null) AppState.FloatingItems.RemoveFloatingElement(fe);
                p.Data.Remove("FloatingElement");
            }
            p.PositionChanged -= p_PositionChanged;

            if (p.Data.ContainsKey("layer")) p.Data.Remove("layer");
            if (p.Data.ContainsKey("graphic"))
            {
                var g = p.Data["graphic"] as PoiGraphic;
                g.Stop();
                //Execute.OnUIThread(() =>
                {
                    l.Graphics.Remove(g);
                    if (g.LabelGraphic != null) l.Graphics.Remove(g.LabelGraphic);
                    if (g.ImageGraphic != null) l.Graphics.Remove(g.ImageGraphic);
                    if (g.InnerTextGraphic != null) l.Graphics.Remove(g.InnerTextGraphic);
                }
                //);
                p.Data.Remove("graphic");
            }
            if (p.Data.ContainsKey("graphics"))
            {
                var g = p.Data["graphics"] as List<PoiGraphic>;
                if (g != null)
                    foreach (var geom in g)
                    {
                        geom.Stop();
                        Execute.OnUIThread(() =>
                        {
                            l.Graphics.Remove(geom);
                            if (geom.LabelGraphic     != null) l.Graphics.Remove(geom.LabelGraphic);
                            if (geom.ImageGraphic     != null) l.Graphics.Remove(geom.ImageGraphic);
                            if (geom.InnerTextGraphic != null) l.Graphics.Remove(geom.InnerTextGraphic);
                        });
                        p.Data.Remove("graphics");
                    }
            }
            base.RemovePoi(p);
        }

        private new void AddPoi(PoI p)
        {
            try
            {
                p.UpdateAnalysisStyle();
                if (string.IsNullOrEmpty(p.Layer) && p.PoiType != null) p.Layer = p.PoiType.Layer;
                var l = FindPoiLayer(p);
                if (!string.IsNullOrEmpty(p.PoiTypeId) && p.PoiType == null)
                    p.PoiType = Service.PoITypes.FirstOrDefault(k => (k).ContentId == p.PoiTypeId) as PoI;

                if (p.PoiType == null)
                {
                    p.PoiType = new PoI
                    {
                        StrokeColor = Colors.White,
                        Service     = service,
                        StrokeWidth = 2,
                        DrawingMode = DrawingModes.Polygon
                    };
                }

                if (p.Data == null) p.Data = new Dictionary<string, object>();
                p.Data["layer"] = this;

                var g = new StaticGraphic { Service = Service, Poi = p };

                if (p.DrawingMode == DrawingModes.MultiPolygon)
                {
                    var converted = p.WktText.ConvertFromWkt();
                    if (converted is MultiPolygon) {
                        // NOTE Others have not been implemented, such as MultiPoint or Path
                        var mp = converted as MultiPolygon;
                        var gs = new List<StaticGraphic>();
                        foreach (var poly in mp.Polygons) 
                        {
                            g        = new StaticGraphic {Service = Service, Poi = p};
                            ConvertPolygonToGraphic(poly, g);
                            g.Layer  = this;
                            g.Poi    = p;
                            g.Symbol = new SimpleFillSymbol 
                            {
                                BorderBrush     = new SolidColorBrush(p.NEffectiveStyle.StrokeColor.Value),
                                BorderThickness = p.NEffectiveStyle.StrokeWidth.Value,
                                Fill            = new SolidColorBrush(p.NEffectiveStyle.FillColor.Value) { Opacity = p.NEffectiveStyle.FillOpacity.Value }
                            };

                            l.Graphics.Add(g);
                            gs.Add(g);
                        }
                        p.Data["graphics"] = gs;
                    }
                    else if (converted is MultiLineString) {
                        var ml = converted as MultiLineString;
                        var gs = new List<StaticGraphic>();
                        foreach (var line in ml.Lines)
                        {
                            g        = new StaticGraphic { Service = Service, Poi = p };
                            ConvertPolylineToGraphic(line, g);
                            g.Layer  = this;
                            g.Poi    = p;
                            g.Symbol = new SimpleFillSymbol
                            {
                                BorderBrush     = new SolidColorBrush(p.NEffectiveStyle.StrokeColor.Value),
                                BorderThickness = p.NEffectiveStyle.StrokeWidth.Value,
                                Fill            = new SolidColorBrush(p.NEffectiveStyle.FillColor.Value) { Opacity = p.NEffectiveStyle.FillOpacity.Value }
                            };

                            l.Graphics.Add(g);
                            gs.Add(g);
                        }
                        p.Data["graphics"] = gs;
                    }
                }
                else
                {
                    g = new StaticGraphic { Service = Service, Poi = p };
                    GetGraphic(p, ref g);
                    l.Graphics.Add(g);
                    p.Data["graphic"] = g;
                }
                p.PositionChanged += p_PositionChanged;

                var height = p.NEffectiveStyle.IconHeight == null ? 32 : p.NEffectiveStyle.IconHeight.Value;
                var width  = p.NEffectiveStyle.IconWidth  == null ? 32 : p.NEffectiveStyle.IconWidth.Value;
                if (p.NEffectiveStyle.DrawingMode.Value == DrawingModes.Point)
                {
                    if (!string.IsNullOrEmpty(p.NEffectiveStyle.Icon))
                    {
                        //var ig = new StaticGraphic {Poi = p, Geometry = g.Geometry, Service = Service};
                        var ig = new Graphic { Geometry = g.Geometry };
                        if (p.NEffectiveStyle.IconUri.OriginalString.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                        {
                            p.NEffectiveStyle.Picture = new BitmapImage(p.NEffectiveStyle.IconUri);
                        }
                        else
                        {
                            var s = Service.MediaFolder + p.NEffectiveStyle.Icon;
                            ig.Attributes["staticgraphic"] = g;
                            if (service.store.HasFile(s))
                            {
                                p.NEffectiveStyle.Picture = new BitmapImage(new Uri(s));
                            }
                        }

                        // When image not found display not_found image (so poi is visible on map)
                        /* if (p.NEffectiveStyle.Picture == null)
                        {
                            p.NEffectiveStyle.Picture = LoadImageFromResource("csCommon.Resources.Icons.ImageNotFound.png");
                        } */

                        //Fix by Jeroen: Invalid bitmap, don't add
                        if (p.NEffectiveStyle.Picture == null)
                            return;

                        var sym = new PictureMarkerSymbol
                        {
                            Source = p.NEffectiveStyle.Picture,
                            Width = width,
                            Height = height,
                            OffsetX = width / 2,
                            OffsetY = height / 2,
                        };

                        if (p.Position != null )
                            sym.Angle = p.Position.Course;
                        ig.Symbol = sym;
                        l.Graphics.Add(ig);
                        ig.MouseLeftButtonDown += (e, st) => g.PoiGraphic_MouseLeftButtonDown(e, st);
                        g.ImageGraphic = ig;
                    }
                }

                if (p.NEffectiveStyle.TitleMode.HasValue && p.NEffectiveStyle.TitleMode.Value != TitleModes.None)
                {
                    var sym = new TextSymbol
                    {
                        Text       = p.Labels.ContainsKey(p.NEffectiveStyle.NameLabel) ? p.Labels[p.NEffectiveStyle.NameLabel] : string.Empty,
                        FontSize   = 12,
                        Foreground = Brushes.Black, 
                        //OffsetX  = -p.Name.Length * 24,
                        OffsetY    = height / -2
                    };
                    
                    var tg = new Graphic { Geometry = g.BaseGeometry, Symbol = sym };
                    tg.Attributes["staticgraphic"] = g;
                    l.Graphics.Add(tg);
                    g.LabelGraphic = tg;
                }

                if (!string.IsNullOrEmpty(p.InnerText))
                {
                    var sym = new TextSymbol
                    {
                        Text       = p.InnerText,
                        FontSize   = 16,
                        Foreground = new SolidColorBrush(p.NEffectiveStyle.InnerTextColor.Value),
                        OffsetX    = (p.InnerText.Length * 5.0),
                        OffsetY    = 8,
                        FontWeight = FontWeights.ExtraBold
                    };

                    if (p.DrawingMode == DrawingModes.Line || p.DrawingMode == DrawingModes.Polyline)
                    {
                        var p0               = p.Points[0];
                        var p1               = p.Points[1];
                        var xdelta           = (p1.X - p0.X);
                        var ydelta           = (p1.Y - p0.Y);
                        const double rad2Deg = 180.0 / Math.PI;
                        var angle            = rad2Deg * Math.Atan2(xdelta, ydelta);
                        sym.OffsetX -= 10 * Math.Cos(angle / rad2Deg);
                        sym.OffsetY -= 10 * Math.Sin(angle / rad2Deg);

                        //angle = (angle + 360.0)%360.0;
                        ////Item is either N or S
                        //if ((angle < 20 || angle > 340) || (angle > 160 && angle < 200))
                        //    sym.OffsetY += 10;
                        ////Item is either NW or SW or NE or SE
                        //else if ((angle > 20 && angle < 70) || (angle > 200 && angle < 250))
                        //{
                        //    sym.OffsetX += 10;
                        //    sym.OffsetY += 10;
                        //}
                        ////Item is either W or E
                        //else if ((angle > 70 && angle < 110) || (angle > 250 && angle < 290))
                        //    sym.OffsetX += 10;
                        ////Item is either NW or SW or NE or SE
                        //else if ((angle > 110 && angle < 160) || (angle > 290 && angle < 340))
                        //{
                        //    sym.OffsetX += 10;
                        //    sym.OffsetY += 10;
                        //}
                        ////                        else glabel = "dunno!";


                    }
                    var tg = new Graphic { Geometry = g.BaseGeometry, Symbol = sym };
                    tg.MouseLeftButtonDown += (e, st) => g.PoiGraphic_MouseLeftButtonDown(e, st);
                    l.Graphics.Add(tg);
                    g.InnerTextGraphic = tg;
                }

                g.UpdateScale();

                base.AddPoi(p);
                //p.UpdateAnalysisStyle();
            }
            catch (Exception e)
            {
                Logger.Log("dsStaticLayer.AddPoi", "Error adding graphic", e.Message, Logger.Level.Error);
            }
        }

        public new void UpdatePosition(BaseContent p) {
            if (p.Position == null) return;
            try
            {
                base.UpdatePosition(p);
                if (!p.NEffectiveStyle.Visible.Value) return;
                var m = mercator.FromGeographic(p.Position.ToMapPoint()) as MapPoint;
                if (!p.Data.ContainsKey("graphic")) return;
                Execute.OnUIThread(() =>
                {
                    var sg = ((StaticGraphic)p.Data["graphic"]);
                    sg.SetGeometry(m);
                    if (sg.ImageGraphic == null) return;
                    var pms = (PictureMarkerSymbol) sg.ImageGraphic.Symbol;
                    sg.ImageGraphic.Geometry = m;
                    if (p.Position != null && Math.Abs(pms.Angle - p.Position.Course) > 0.001) pms.Angle = p.Position.Course;
                });
            }
            catch (SystemException e) {
                Logger.Log("dsStaticLayer.UpdatePosition", "Unhandled exception", e.Message, Logger.Level.Error);                
            }
        }

        void p_PositionChanged(object sender, PositionEventArgs e)
        {
            var p = (PoI)sender;
            UpdatePosition(p);
        }

        private void GetGraphic(PoI p, ref StaticGraphic g)
        {
            g.Layer = this;
            g.Poi = p;

            switch (p.DrawingMode)
            {
                case DrawingModes.Point:
                    if (p.Position != null)
                    {
                        var m = mercator.FromGeographic(p.Position.ToMapPoint()) as MapPoint;
                        g.SetGeometry(m);

                        //if (p.NEffectiveStyle.FillColor.HasValue)
                        Debug.Assert(p.NEffectiveStyle.IconHeight != null, "NEffectiveStyle.IconHeight is null, should always have default value (PoIStyle.GetBasicStyle()).  ");
                        Debug.Assert(p.NEffectiveStyle.IconWidth != null, "NEffectiveStyle.IconWidth is null, should always have default value. (PoIStyle.GetBasicStyle())");
                        g.Symbol = new SimpleMarkerSymbol
                        {
                            Style = SimpleMarkerSymbol.SimpleMarkerStyle.Circle,
                            Size = Math.Min((double)p.NEffectiveStyle.IconHeight, (double)Math.Min((double)p.NEffectiveStyle.IconWidth, (double)26.0))+4,
                            Color = new SolidColorBrush(p.NEffectiveStyle.FillColor.Value) { Opacity = p.NEffectiveStyle.FillOpacity.Value },
                        };

                    }
                    break;
                case DrawingModes.Image:
                    if (p.Position != null)
                    {
                        var m = mercator.FromGeographic(p.Position.ToMapPoint()) as MapPoint;
                        g.SetGeometry(m);
                    }
                    if (p.NEffectiveStyle.IconUri.OriginalString.ToLower().StartsWith("http"))
                    {
                        p.NEffectiveStyle.Picture = new BitmapImage(p.NEffectiveStyle.IconUri);
                    }
                    else
                    {
                        var s = Service.MediaFolder + p.NEffectiveStyle.Icon;
                        if (service.store.HasFile(s))
                        {
                            p.NEffectiveStyle.Picture = new BitmapImage(new Uri(s));
                        }
                    }
                    // When image not found display not_found image (so poi is visible on map)
                    /* if (p.NEffectiveStyle.Picture == null)
                    {
                        p.NEffectiveStyle.Picture = LoadImageFromResource("csCommon.Resources.Icons.ImageNotFound.png");
                    } */

                    if (p.NEffectiveStyle.Picture != null && p.NEffectiveStyle.IconWidth != null)
                    {
                        g.Symbol = new PictureMarkerSymbol
                        {
                            Source = p.NEffectiveStyle.Picture,
                            Width = p.NEffectiveStyle.IconWidth.Value,
                            Height = p.NEffectiveStyle.IconHeight.Value,
                            OffsetX = p.NEffectiveStyle.IconWidth.Value/2,
                            OffsetY = p.NEffectiveStyle.IconHeight.Value/2,
                            Opacity = p.NEffectiveStyle.FillOpacity.Value
                        };
                    }

                    break;
                case DrawingModes.Freehand:
                    if (p.FillColor.A == 0)
                        ConvertPointsToPolyline(p, g);
                    else
                        ConvertPointsToPolygon(p, g);
                    break;
                case DrawingModes.Polyline:
                    //CreateLineSymbol(p, g);
                    g.Symbol = new SimpleLineSymbol
                    {
                        Width = p.NEffectiveStyle.StrokeWidth.Value,
                        Color = new SolidColorBrush(p.NEffectiveStyle.StrokeColor.Value) { Opacity = p.NEffectiveStyle.StrokeOpacity.Value }
                    };
                    ConvertPointsToPolyline(p, g);
                    break;
                case DrawingModes.Polygon:
                    g.Symbol = new SimpleFillSymbol
                    {
                        BorderBrush = new SolidColorBrush(p.NEffectiveStyle.StrokeColor.Value),
                        BorderThickness = p.NEffectiveStyle.StrokeWidth.Value,
                        Fill = new SolidColorBrush(p.NEffectiveStyle.FillColor.Value) { Opacity = p.NEffectiveStyle.FillOpacity.Value }
                    };
                    if (p.Geometry != null)
                        ConvertGeometryToPolygon(p, g);
                    else
                        ConvertPointsToPolygon(p, g);
                    break;
                case DrawingModes.MultiPolygon:
                    g.Symbol = new SimpleFillSymbol
                    {
                        BorderBrush = new SolidColorBrush(p.NEffectiveStyle.StrokeColor.Value),
                        BorderThickness = p.NEffectiveStyle.StrokeWidth.Value,
                        Fill = new SolidColorBrush(p.NEffectiveStyle.FillColor.Value) { Opacity = p.NEffectiveStyle.FillOpacity.Value }
                    };
                    break;
                case DrawingModes.Circle:
                    ConvertPointsToPolygon(p, g);
                    break;
            }
        }
        /*
        private BitmapImage LoadImageFromResource(string pResourceUrl)
        {

            var assembly = Assembly.GetExecutingAssembly();
            var resources = assembly.GetManifestResourceNames();
            using (Stream stream = assembly.GetManifestResourceStream(pResourceUrl))
            {

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                return bitmap;
            }
        }*/

        #region ConvertPoints helpers

        private PointCollection ConvertPointsToPointCollection(BaseContent p)
        {
            if (p.Points == null || p.Points.Count == 0) return null;
            var pc = new PointCollection();
            foreach (var po in p.Points)
            {
                pc.Add(mercator.FromGeographic(new MapPoint(po.X, po.Y)) as MapPoint);
            }
            return pc;
        }

        private void ConvertPointsToPolygon(BaseContent p, PoiGraphic g)
        {
            var pc = ConvertPointsToPointCollection(p);
            if (pc == null) return;
            var polygon = new Polygon();
            pc.Add(pc[0]); // Close the loop
            polygon.Rings.Add(pc);
            g.SetGeometry(polygon);
        }

        private void ConvertGeometryToPolygon(PoI p, PoiGraphic g)
        {
            var polygon = new Polygon();
            if (p.Geometry is csCommon.Types.Geometries.Polygon)
            {
                var geom = p.Geometry as csCommon.Types.Geometries.Polygon;
                foreach (var ls in geom.LineStrings)
                {
                    var pc = new PointCollection();
                    foreach (var ps in ls.Line)
                    {
                        pc.Add(mercator.FromGeographic(new MapPoint(ps.X, ps.Y)) as MapPoint);
                    }
                    if (pc.First().X != pc.Last().X || pc.First().Y != pc.Last().Y)
                        pc.Add(pc.First());
                    polygon.Rings.Add(pc);
                }
                g.SetGeometry(polygon);
            }
            else return;
        }

        private void ConvertPolygonToGraphic(csCommon.Types.Geometries.Polygon p, PoiGraphic g)
        {
            var polygon = new Polygon();
            foreach (var ls in p.LineStrings)
            {
                var pc = new PointCollection();
                foreach (var point in ls.Line)
                {
                    pc.Add(mercator.FromGeographic(new MapPoint(point.X, point.Y)) as MapPoint);
                }
                if (pc.First().X != pc.Last().X || pc.First().Y != pc.Last().Y)
                    pc.Add(pc.First());
                polygon.Rings.Add(pc);
            }
            g.SetGeometry(polygon);
        }

        /// <summary>
        /// Convert a polyline to a graphic.
        /// NOTE Although we could have used a Polyline to create the graphic, it would mean that we cannot specify the fill color,
        /// which is why I've chosen to convert it to a Polygon.
        /// </summary>
        /// <param name="ls"></param>
        /// <param name="g"></param>
        private void ConvertPolylineToGraphic(LineString ls, PoiGraphic g)
        {
            var polygon = new Polygon();
            var pc = new PointCollection();
            foreach (var point in ls.Line)
            {
                pc.Add(mercator.FromGeographic(new MapPoint(point.X, point.Y)) as MapPoint);
            }
            //if (pc.First().X != pc.Last().X || pc.First().Y != pc.Last().Y)
            //    pc.Add(pc.First());
            polygon.Rings.Add(pc);
            g.SetGeometry(polygon);
        }


        private void ConvertPointsToPolyline(BaseContent p, PoiGraphic g)
        {
            var pc = ConvertPointsToPointCollection(p);
            if (pc == null) return;
            var pol = new Polyline();
            pol.Paths.Add(pc);
            g.SetGeometry(pol);
        }

        #endregion ConvertPoints helpers

        private void ToGeographic(ref double mercatorX_lon, ref double mercatorY_lat)
        {
            if (Math.Abs(mercatorX_lon) < 180 && Math.Abs(mercatorY_lat) < 90)
                return;

            if ((Math.Abs(mercatorX_lon) > 20037508.3427892) || (Math.Abs(mercatorY_lat) > 20037508.3427892))
                return;

            double x = mercatorX_lon;
            double y = mercatorY_lat;
            double num3 = x / 6378137.0;
            double num4 = num3 * 57.295779513082323;
            double num5 = Math.Floor((num4 + 180.0) / 360.0);
            double num6 = num4 - (num5 * 360.0);
            double num7 = 1.5707963267948966 - (2.0 * Math.Atan(Math.Exp((-1.0 * y) / 6378137.0)));
            mercatorX_lon = num6;
            mercatorY_lat = num7 * 57.295779513082323;
        }

        private void ToWebMercator(ref double mercatorX_lon, ref double mercatorY_lat)
        {
            if ((Math.Abs(mercatorX_lon) > 180 || Math.Abs(mercatorY_lat) > 90))
                return;

            double num = mercatorX_lon * 0.017453292519943295;
            double x = 6378137.0 * num;
            double a = mercatorY_lat * 0.017453292519943295;

            mercatorX_lon = x;
            mercatorY_lat = 3189068.5 * Math.Log((1.0 + Math.Sin(a)) / (1.0 - Math.Sin(a)));
        }

        public void End()
        {
            IsStarted = false;
            Service.PoIs.CollectionChanged -= PoisCollectionChanged;
            Service.PoiUpdated -= ServicePoiUpdated;
            // Wasn't called in start with += , so comment out
            //AppState.TimelineManager.FocusTimeUpdated -= TimelineManagerFocusTimeChanged;
            AppState.ViewDef.MapControl.ExtentChanged -= MapControlExtentChanged;
            Service.Initialized -= ServiceInitialized;
            foreach (dsStaticSubLayer s in subLayers.Values) s.Stop();
        }

        private void MapControlExtentChanged(object sender, ExtentEventArgs e)
        {
            Service.SetExtent(AppState.ViewDef.WorldExtent);
            //Console.WriteLine("Count=" + s.Result.Count.ToString());
        }

        public dsStaticSubLayer FindPoiLayer(PoI p)
        {
            var n = string.IsNullOrEmpty(p.Layer) ? ID : p.Layer;
            return FindPoiLayer(n);
        }

        public dsStaticSubLayer FindPoiLayer(string name) {
            return subLayers.ContainsKey(name) ? subLayers[name] : AddChildLayer(name);
        }

        private dsStaticSubLayer AddChildLayer(string name) {
            var npl = new dsStaticSubLayer {ID = name, Parent = this, Visible = Service.Settings.SublayersVisible};
            subLayers[name] = npl;
            ChildLayers.Add(npl);
            CheckLayerOrder();
            npl.Initialize();
            if (UseClusterer) npl.Clusterer = new FlareClusterer();
            npl.Start();
            AppStateSettings.Instance.TriggerScriptCommand(this, ScriptCommands.UpdateLayers);
            return npl;
        }

        public Dictionary<string, dsPoiLayer> PoiLayers { get; set; }

        public dsStaticSubLayer GetSubLayer(string name)
        {
            if (name == null) return null;
            return subLayers.ContainsKey(name) ? subLayers[name] : null;
        }

        public new List<System.Windows.Controls.MenuItem> GetMenuItems()
        {
            var l = new List<System.Windows.Controls.MenuItem>();

            if (service.IsSubscribed)
            {
                if (IsTabActive)
                {
                    var closetab = MenuHelpers.CreateMenuItem("Close Tab", MenuHelpers.FolderIcon);
                    closetab.Click += (e, f) => CloseTab();
                    l.Add(closetab);
                }
                if (!service.IsLocal) return l;
                if (service.Mode == Mode.client)
                {
                    var shareonline = MenuHelpers.CreateMenuItem("Start Sharing", MenuHelpers.OnlineIcon);
                    shareonline.Click += (e, f) => service.MakeOnline();
                    l.Add(shareonline);

                    if (AppState.Imb.ActiveGroup == null) return l;
                    var sharegroup = MenuHelpers.CreateMenuItem("Share with group", MenuHelpers.OnlineIcon);
                    sharegroup.Click += (e, f) => service.ShareInGroup();
                    l.Add(sharegroup);
                }
                else
                {
                    var makelocal = MenuHelpers.CreateMenuItem("Stop Sharing", MenuHelpers.OnlineIcon, Brushes.Gray);
                    makelocal.Click += (e, f) => service.MakeLocal();
                    l.Add(makelocal);
                }
            }
            else if (service.IsLocal)
            {
                var startshare = MenuHelpers.CreateMenuItem("Start & Share", MenuHelpers.OnlineIcon);
                startshare.Click += (e, f) => StartShare();
                l.Add(startshare);
            }

            return l;
        }

        public void StartShare()
        {
            Start(true);
        }

        public bool IsOnline
        {
            get { return (service != null && service.Mode == Mode.server); }
        }

        public bool IsShared
        {
            get { return (service != null && service.IsShared && !IsOnline); }
        }

    }

    public class AccelerableSimpleMarkerSymbol : MarkerSymbol, IJsonSerializable
    {
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(SolidColorBrush), typeof(AccelerableSimpleMarkerSymbol),
                new PropertyMetadata(default(SolidColorBrush)));

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register("Size", typeof(double), typeof(AccelerableSimpleMarkerSymbol),
                new PropertyMetadata(default(double)));

        public static readonly DependencyProperty MarkerStyleProperty =
            DependencyProperty.Register("MarkerStyle", typeof(SimpleMarkerSymbol.SimpleMarkerStyle),
                typeof(AccelerableSimpleMarkerSymbol),
                new PropertyMetadata(default(SimpleMarkerSymbol.SimpleMarkerStyle)));

        public SolidColorBrush Color
        {
            get { return (SolidColorBrush)GetValue(ColorProperty); }

            set { SetValue(ColorProperty, value); }
        }

        public double Size
        {
            get { return (double)GetValue(SizeProperty); }

            set { SetValue(SizeProperty, value); }
        }

        public SimpleMarkerSymbol.SimpleMarkerStyle MarkerStyle
        {
            get { return (SimpleMarkerSymbol.SimpleMarkerStyle)GetValue(MarkerStyleProperty); }

            set { SetValue(MarkerStyleProperty, value); }
        }


        #region IJsonSerializable Members

        public string ToJson()
        {
            var simpleMarker = new SimpleMarkerSymbol
            {
                Color = Color,
                Size = Size,
                Style = MarkerStyle
            };

            return simpleMarker.ToJson();
        }

        #endregion
    }
}