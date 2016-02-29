using Caliburn.Micro;
using csCommon.Types;
using csGeoLayers.Content.Lookr;
using csGeoLayers.Content.Panoramio;
using csGeoLayers.GeoRSS;
using csShared;
using csShared.Controls.Popups.MenuPopup;
using csShared.Geo;
using DataServer;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Toolkit.DataSources;
using Microsoft.Surface.Presentation.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using IContent = csShared.Geo.Content.IContent;
using MenuItem = System.Windows.Controls.MenuItem;

namespace csCommon
{
    public interface ILayerSelection { }

    [Export(typeof(ILayerSelection))]
    public class LayersViewModel : Screen, ILayerSelection
    {
        private sLayer startLayer;

        public sLayer StartLayer
        {
            get { return startLayer; }
            set { startLayer = value; NotifyOfPropertyChange(() => StartLayer); }
        }

        private int selectedTab;

        public int SelectedTab
        {
            get { return selectedTab; }
            set { selectedTab = value; NotifyOfPropertyChange(() => SelectedTab); }
        }

        public double CornerRadius
        {
            get
            {
                return AppState.CornerRadius;
            }
        }

        private List<IContent> content;

        public List<IContent> Content
        {
            get { return content; }
            set { content = value; NotifyOfPropertyChange(() => Content); }
        }

        private string filter;

        public string Filter
        {
            get { return filter; }
            set
            {
                filter = value;
                NotifyOfPropertyChange(() => Filter);
                UpdateContent();
            }
        }

        public void Init()
        {
            //AppState.ViewDef.Content.Add(new FlightTrackerContent {Name = "Flight Tracker Netherlands"});
            AppState.IsOnlineChanged += (e, f) =>
            {
                NotifyOfPropertyChange(() => AvailableTemplates);
                UpdateContent();
            };
            AppState.ViewDef.Content.Add(new PanoramioContent { Name = "Panoramio" });
            AppState.ViewDef.Content.Add(new LookrContent     { Name = "Lookr"     });
            //AppState.ViewDef.Content.Add(new GeoRSSContent
            //{
            //    Name = "Earthquakes Past Hour (M1+)",
            //    Location =
            //        new Uri(
            //        "http://earthquake.usgs.gov/earthquakes/catalogs/eqs1hour-M1.xml"),
            //    Folder = "Disaster/Earthquakes",
            //    IconUri =
            //        new Uri(
            //        @"http://maps.google.com/mapfiles/kml/shapes/earthquake.png")
            //});
            //AppState.ViewDef.Content.Add(new GeoRSSContent
            //{
            //    Name = "Earthquakes Past Day (M1+)",
            //    Location =
            //        new Uri(
            //        "http://earthquake.usgs.gov/earthquakes/catalogs/eqs1day-M1.xml"),
            //    Folder = "Disaster/Earthquakes",
            //    IconUri =
            //        new Uri(
            //        @"http://maps.google.com/mapfiles/kml/shapes/earthquake.png")
            //});
            //AppState.ViewDef.Content.Add(new GeoRSSContent
            //{
            //    Name = "Earthquakes 7 Days (M5+)",
            //    Location =
            //        new Uri(
            //        "http://earthquake.usgs.gov/earthquakes/catalogs/eqs7day-M5.xml"),
            //    Folder = "Disaster/Earthquakes",
            //    IconUri =
            //        new Uri(
            //        @"http://maps.google.com/mapfiles/kml/shapes/earthquake.png")
            //});
            //AppState.ViewDef.Content.Add(new GeoRSSContent
            //{
            //    Name = "Reuters News",
            //    Location =
            //        new Uri(
            //        "http://ws.geonames.org/rssToGeoRSS?feedUrl=http://feeds.reuters.com/reuters/worldNews"),
            //    Folder = "News",
            //    IconUri =
            //        new Uri(@"http://www.borealbirds.org/images/icon-reuters-logo.gif"),
            //    IconSize = 32
            //});

            //AppState.ViewDef.Content.Add(new WmsContent()
            //{
            //    Name = "BAG Data",
            //    Location = "http://geodata.nationaalgeoregister.nl/bagviewer/wms?",
            //    Layers = new[] { "pand", "ligplaats", "standplaats", "verblijfsobject" }

            //});

            //AppState.ViewDef.Content.Add(new WmsContent()
            //{
            //    Name = "Vaarwegen",
            //    Location = "http://www.vaarweginformatie.nl/wfswms/services?",
            //    Layers = new[] { "Bridge", "Fairway" }
            //});

            //AppState.ViewDef.Content.Add(new KmlContent
            //{
            //    Name = "Global Disaster Alert and Coordination system",
            //    Location = new Uri("http://www.gdacs.org/xml/gdacs.kml"),
            //    Folder = "Disaster"
            //});

            AppState.ViewDef.Content.Add(new ArcgisOnlineContentPlugin { Name = "ArcGis" });

            //AppState.ViewDef.Content.Add(new FlexibleRainRadarContent {
            //    Name = "RainRadar EU",
            //    Config = new Dictionary<string, string> {
            //        {"interval", "15"},
            //        {"tlLat", "-14.9515"},
            //        {"tlLon", "41.4389"},
            //        {"brLat", "20.4106"},
            //        {"brLon", "59.9934"}, 
            //        {"baseUrl", "http://134.221.210.43:8000/BuienRadarService/RainImage/eu/warped/"}
            //    }
            //});
            #region old

            //AppState.ViewDef.Content.Add(new AirportContent() { Name = "Airports Worldwide" });
            //AppState.ViewDef.Content.Add(new GeoRSSContent() { Name = "C2000 masten", Location = new Uri("file://" + Directory.GetCurrentDirectory() + @"\Content\Data\c2000.rss"), Folder = "Disaster/Earthquakes", IconUri = new Uri(@"http://maps.google.com/mapfiles/kml/shapes/earthquake.png") });


            //AppState.ViewDef.Content.Add(new KmlContent()
            //                                 {
            //                                     Name="Tno",
            //                                     Location = new Uri("http://nationaalgeoregister.nl/geonetwork/srv/nl/google.kml?uuid=f646dfb9-5bf6-eab9-042b-cab6ff2dc275&layers=M11M0561"),
            //                                     Folder = "Ondergrond"
            //                                 });
            //AppState.ViewDef.Content.Add(new WmsContent()
            //                                 {
            //                                     //http://geoservices.cbs.nl/arcgis/services/BestandBodemGebruik2008/MapServer/WMSServer
            //                                     Name = "Bodemgebruik 2008",
            //                                     Location =
            //                                         "http://mesonet.agron.iastate.edu/cgi-bin/wms/nexrad/n0r.cgi",
            //                                     Folder = "Bodemgebruik",

            //                                 });

            //AppState.ViewDef.Content.Add(new NO2Content());

            //AppState.ViewDef.Content.Add(new FlexibleRainRadarContent()
            //{
            //    Name = "RainRadar NL",
            //    Config = new Dictionary<string, string> { { "interval", "5" }, { "tlLat", "0" }, { "tlLon", "55.974" }, { "brLat", "10.856" }, { "brLon", "48.895" }, { "baseUrl", "http://134.221.210.43:8000/BuienRadarService/RainImage/nl/warped/" } }
            //});

            //AppState.ViewDef.Content.Add(new TwitterContent()
            //                                 {
            //                                     Name = "Twitter"
            //                                 });

            #endregion
        }

        public void UpdateContent()
        {
            Content = string.IsNullOrEmpty(Filter) 
                ? AppStateSettings.Instance.ViewDef.Content.Where(k => AppState.IsOnline || (!AppState.IsOnline && !k.IsOnline)).ToList() 
                : AppStateSettings.Instance.ViewDef.Content.Where(k => k.Name.ToLower().Contains(Filter.ToLower())).ToList();
        }

        public List<PoiService> AvailableTemplates
        {
            get { return AppState.DataServer!=null ? AppState.DataServer.Templates.ToList() : null; }
        }

        public async void AddTemplate(PoiService selectedTemplate)
        {
            //var i = 1;
            //var selectedTemplateName = selectedService.Name;
            //var newServiceName = selectedTemplateName + i;
            //while (AppState.DataServer.Services.Any(s => s.Name.Equals(newServiceName))) newServiceName = selectedTemplateName + ++i;

            var folder = Path.GetDirectoryName(selectedTemplate.FileName) ?? string.Empty;
            // Make sure you save it, otherwise you cannot subscribe to it.
            if (await PoiService.CreateTemplateBasedService(folder, Path.Combine(folder, selectedTemplate.Name)) == null) return;
            SelectedTab = 0;
        }

        private string previous;
        public string Previous
        {
            get { return previous; }
            set {
                previous = value;
                NotifyOfPropertyChange(() => Previous);
            }
        }

        private DispatcherTimer dirtyTimer;

        public GroupLayer PreviousLayer { get; set; }

        public static AppStateSettings AppState { get { return AppStateSettings.Instance; } }

        private LayersView view;

        private bool dirty;

        public FloatingElement Element { get; set; }


        public MapViewDef ViewDef
        {
            get
            {
                return AppState.ViewDef;
            }
        }

        public LayersViewModel()
        {
            // Default caption
            Caption = "Layers";
        }

        public static void ParseWmsLayers(Layer layer, WmsLayer.LayerInfo layerinfo, ref sWmsLayer parent)
        {
            var wms = new sWmsLayer
            {
                Layer             = layer,
                Title             = layerinfo.Name,
                LayerInfo         = layerinfo,
                BaseLayer         = parent.BaseLayer,
                IsTabAvailable    = layer is ITabLayer,
                IsConfigAvailable = layer is ISettingsLayer
            };
            parent.Children.Add(wms);

            if (layerinfo.ChildLayers.Count <= 0) return;
            foreach (var sl in layerinfo.ChildLayers)
            {
                ParseWmsLayers(layer, sl, ref wms);
            }
        }

        public void ParseLayers(Layer layer, ref sLayer parent, string relativePath, bool include = true)
        {
            try
            {
                if (layer is IIgnoreLayer) return;
                var nl = parent;
                if (include)
                {
                    var wmsLayer = layer as WmsLayer;
                    if (wmsLayer != null)
                    {
                        wmsLayer.Initialized -= w_Initialized;
                        wmsLayer.Initialized += w_Initialized;

                        var wms = new sWmsLayer
                        {
                            Layer             = layer,
                            Title             = layer.ID,
                            Parent            = parent,
                            IsTabAvailable    = layer is ITabLayer,
                            IsConfigAvailable = layer is ISettingsLayer
                        };
                        wms.BaseLayer = wms;
                        parent.Children.Add(wms);
                        wms.Path = relativePath + "\\" + wmsLayer.ID;
                        if (wmsLayer.LayerList.Count > 0)
                        {
                            foreach (var sl in wmsLayer.LayerList.OrderBy(l => l.Name))
                            {
                                ParseWmsLayers(layer, sl, ref wms);
                            }
                        }
                    }
                    else
                    {
                        nl = new sLayer
                        {
                            Layer             = layer,
                            Title             = layer.ID,
                            IsTabAvailable    = layer is ITabLayer,
                            IsConfigAvailable = layer is ISettingsLayer,
                            IsService         = layer is IStartStopLayer,
                            Parent            = parent,
                            Path              = relativePath + "\\" + layer.ID
                        };
                        var nl1 = nl;
                        layer.PropertyChanged += (s, f) =>
                        {
                            if (f.PropertyName == "Visible")
                                nl1.Visible = layer.Visible;
                        };
                        var onlineLayer = layer as IOnlineLayer;
                        if (onlineLayer != null)
                        {
                            nl.IsOnline = onlineLayer.IsOnline;
                            nl.IsShared = onlineLayer.IsShared;
                        }
                        parent.Children.Add(nl);
                    }
                }

                var gl = layer as GroupLayer;
                if (gl == null) return;
                foreach (var cl in gl.ChildLayers.OrderBy(l => l.ID))
                {
                    //sLayer sl = new sLayer() {Layer = cl};
                    //nl.Children.Add(sl);
                    ParseLayers(cl, ref nl, relativePath + "\\" + cl.ID);
                }

                var children = layer as ILayerWithMoreChildren;
                if (children != null)
                {
                    foreach (var cl in children.Children.OrderBy(l => l.ID))
                    {
                        //sLayer sl = new sLayer() {Layer = cl};
                        //nl.Children.Add(sl);
                        ParseLayers(cl, ref nl, relativePath + "\\" + cl.ID);
                    }
                }
                gl.ChildLayers.CollectionChanged -= MakeDirty;
                gl.ChildLayers.CollectionChanged += MakeDirty;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void ParseEsriLayers(Layer layer, ref sLayer parent, string relativePath, bool include = true)
        {
            try
            {
                if (layer is IIgnoreLayer) return;
                var nl = parent;
                if (include)
                {
                    var wmsLayer = layer as WmsLayer;
                    if (wmsLayer != null)
                    {
                        wmsLayer.Initialized -= w_Initialized;
                        wmsLayer.Initialized += w_Initialized;

                        var wms = new sWmsLayer
                        {
                            Layer = layer,
                            Title = layer.ID,
                            Parent = parent,
                            IsTabAvailable = layer is ITabLayer,
                            IsConfigAvailable = layer is ISettingsLayer
                        };
                        wms.BaseLayer = wms;
                        parent.Children.Add(wms);
                        wms.Path = relativePath + "\\" + wmsLayer.ID;
                        if (wmsLayer.LayerList.Count > 0)
                        {
                            foreach (var sl in wmsLayer.LayerList.OrderBy(l => l.Name))
                            {
                                ParseWmsLayers(layer, sl, ref wms);
                            }
                        }
                    }
                    else
                    {
                        nl = new sLayer
                        {
                            Layer = layer,
                            Title = layer.ID,
                            IsTabAvailable = layer is ITabLayer,
                            IsConfigAvailable = layer is ISettingsLayer,
                            IsService = layer is IStartStopLayer,
                            Parent = parent,
                            Path = relativePath + "\\" + layer.ID
                        };
                        var nl1 = nl;
                        layer.PropertyChanged += (s, f) =>
                        {
                            if (f.PropertyName == "Visible")
                                nl1.Visible = layer.Visible;
                        };
                        var onlineLayer = layer as IOnlineLayer;
                        if (onlineLayer != null)
                        {
                            nl.IsOnline = onlineLayer.IsOnline;
                            nl.IsShared = onlineLayer.IsShared;
                        }
                        parent.Children.Add(nl);
                    }
                }

                var gl = layer as GroupLayer;
                if (gl == null) return;
                foreach (var cl in gl.ChildLayers.OrderBy(l => l.ID))
                {
                    //sLayer sl = new sLayer() {Layer = cl};
                    //nl.Children.Add(sl);
                    ParseLayers(cl, ref nl, relativePath + "\\" + cl.ID);
                }

                var children = layer as ILayerWithMoreChildren;
                if (children != null)
                {
                    foreach (var cl in children.Children.OrderBy(l => l.ID))
                    {
                        //sLayer sl = new sLayer() {Layer = cl};
                        //nl.Children.Add(sl);
                        ParseLayers(cl, ref nl, relativePath + "\\" + cl.ID);
                    }
                }
                gl.ChildLayers.CollectionChanged -= MakeDirty;
                gl.ChildLayers.CollectionChanged += MakeDirty;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void w_Initialized(object sender, EventArgs e)
        {
            dirty = true;
        }

        private void MakeDirty(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            dirty = true;
        }

        [ImportingConstructor]
        public LayersViewModel(CompositionContainer container)
        {
            Caption = "Layers";
        }

        private readonly Dictionary<string, bool> expandedStates = new Dictionary<string, bool>();

        public void UpdateExpandState(sLayer l)
        {
            if (l == null || l.Path==null) return;
            expandedStates[l.Path] = l.IsExpanded;
        }
        
        #region wms
        public void GetWmsLayers(sWmsLayer li, ref List<WmsLayer.LayerInfo> enabled)
        {
            if (li.Children.Any())
            {
                foreach (var l in li.Children) GetWmsLayers((sWmsLayer)l, ref enabled);
            }
            else
            {
                if (li.Visible) enabled.Add(li.LayerInfo);
            }
        }

        public void UpdateWmsLayer(sWmsLayer l)
        {
            // if (!l.Children.Any()) return;
            var enabled = new List<WmsLayer.LayerInfo>();
            foreach (sWmsLayer layerInfo in l.Children)
            {
                GetWmsLayers(layerInfo, ref enabled);
            }
            var wms = (WmsLayer)l.Layer;
            wms.Layers = enabled.Select(k => k.Name).ToArray();
            wms.Refresh();
        }
        #endregion

        public void Checked(sLayer l)
        {
            if (l == null || l.Path==null) return;
            expandedStates[l.Path] = l.IsExpanded;
            var wmsLayer = l as sWmsLayer;
            if (wmsLayer != null) UpdateWmsLayer(wmsLayer.BaseLayer);
            var gl = l.Layer as GraphicsLayer;
            if (gl != null)
            {
                gl.Opacity = 1;
                return;
            }
            var ls = l.Layer as IStartStopLayer;
            if (ls == null) return;
            if (!ls.IsStarted)
                //ThreadPool.QueueUserWorkItem(delegate { 
                ls.Start();
            //});
        }

        public void Unchecked(sLayer l)
        {
            if (l == null || l.Path==null) return;
            expandedStates[l.Path] = l.IsExpanded;
            var wmsLayer = l as sWmsLayer;
            if (wmsLayer != null) UpdateWmsLayer(wmsLayer.BaseLayer);
            var ls = l.Layer as IStartStopLayer;
            if (ls != null && ls.IsStarted && ls.CanStop)
            {
                ls.Stop();
                return;
            }
            var gl = l.Layer as GraphicsLayer;
            if (gl != null) {
                gl.Opacity = 0;
                return;
            }
            // Since we couldn't stop it, turn the visibility back on.
            l.Visible = true;
        }

        public void Config(sLayer l)
        {
            var layer = l.Layer as ISettingsLayer;
            if (layer != null) layer.StartSettings();
        }

        public void Zoom(sLayer l)
        {
            try
            {
                MapViewDef.ZoomTo(l.Layer);
            }
            catch (Exception)
            {
                AppState.TriggerNotification("There is not a valid zoom level for " + l.Layer.ID);
            }
        }

        public void ToggleTab(sLayer l)
        {
            if (l == null) return;
            var tl = (ITabLayer)l.Layer;
            if (tl.IsTabActive)
                tl.OpenTab();
            else tl.CloseTab();
        }

        public void CleanUpLayers(ref sLayer l)
        {
            var tbr = new List<sLayer>();

            for (var i = 0; i < l.Children.Count; i++)
            {
                var c = l.Children[i];

                c.HasMenu = GetMenuItems(c).Any();

                if (c.Children == null || c.Children.Count > 0) continue; //
                
                if (l.Layer is IStartStopLayer && !((IStartStopLayer)l.Layer).IsStarted)
                {
                    tbr.Add(c);
                }

                if (c.Layer is IStartStopLayer && !((IStartStopLayer) c.Layer).IsStarted)
                {
                    c.Children.Clear();
                }

                if (c.Layer is GraphicsLayer && !((GraphicsLayer)c.Layer).Graphics.Any())
                {
                    tbr.Add(c);
                }

                if (c.Layer is GraphicsLayer && l.Children.Count == 1 && l.Title == c.Title)
                {
                    tbr.Add(c);
                }

                //if (c.Layer is IStartStopLayer && c.Children != null && c.Children.Count == 1 && c.Children[0].Title == c.Title)
                //{
                //    l.Children.Add(c);

                //    c.Children.Clear();
                //    tbr.Add(c);
                //}
                //else 
                if (c.Children == null || !c.Children.Any()) continue;
                l.Children.Add(c.Children[0]);
                c.Children.Clear();
                tbr.Add(c);
            }
            
            foreach (var c in tbr)
            {
                l.Children.Remove(c);
            }
            foreach (var t in l.Children)
            {
                var c = t;
                CleanUpLayers(ref c);
            }
            if (l.Path != null && expandedStates.ContainsKey(l.Path)) l.IsExpanded = expandedStates[l.Path];
        }

        private bool updating;

        public void UpdateLayers()
        {
            if (updating) return;
            
            try
            {
                updating = true;
                StartLayer = new sLayer();
                ParseLayers(AppState.ViewDef.AcceleratedLayers, ref startLayer, "AcceleratedLayers", false);
                ParseLayers(AppState.ViewDef.Layers, ref startLayer, "Layers", false);
                ParseLayers(AppState.ViewDef.BaseLayers, ref startLayer, "BaseLayers", true);                
                CleanUpLayers(ref startLayer);
                ParseLayers(AppState.ViewDef.EsriLayers, ref startLayer, "BaseLayers", true);
                NotifyOfPropertyChange(() => StartLayer);
                //Layers = AppState.ViewDef.Layers;
                if (view != null)
                {
                    //view.tvLayers.ItemsSource = StartLayer.Children;
                    view.tvStartLayer.ItemsSource = StartLayer.Children;
                }
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                updating = false;
            }
            


            
        }

        private static IEnumerable<MenuItem> GetMenuItems(sLayer layer)
        {
            var r = new List<MenuItem>();
            var menuLayer = layer.Layer as IMenuLayer;
            if (menuLayer == null) return r;
            var mi = menuLayer.GetMenuItems();
            if (mi != null)
                r.AddRange(mi);
            return r;
        }

        public void LayerMenu(sLayer layer, FrameworkElement element)
        {
            if (layer == null) return;

            var menu = new MenuPopupViewModel
            {
                RelativeElement = element,
                RelativePosition = new Point(25, 0),
                TimeOut = new TimeSpan(0, 0, 0, 5),
                VerticalAlignment = VerticalAlignment.Bottom,
                DisplayProperty = string.Empty
            };

            menu.Items.AddRange(GetMenuItems(layer));

            if (menu.Items.Any()) AppState.Popups.Add(menu);
        }

        public string Caption { get; set; }

        public static void sbSelect()
        {
            //PreviousLayer = Layers;
            //Previous = Layers.ID;
        }

        public Brush AccentBrush
        {
            get { return AppState.AccentBrush; }
        }

        public void AddContent(IContent content)
        {
            content.Add();
            AppState.TriggerNotification(content.Name + " content added");
        }

        public void RemoveContent(IContent content)
        {
            content.Remove();
            AppState.TriggerNotification(content.Name + " content removed");
        }

        public void ConfigContent(IContent content)
        {
            content.Configure();
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            Init();
            this.view = (LayersView)view;
            var a = BaseWPFHelpers.Helpers.FindElementOfTypeUp(this.view, typeof(ScatterViewItem));
            if (a != null)
            {
                Element = a.DataContext as FloatingElement;
            }

            UpdateLayers();

            AppState.ScriptCommand += AppState_ScriptCommand;

            dirtyTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            dirtyTimer.Tick += dirtyTimer_Tick;
            dirtyTimer.Start();

            UpdateContent();
        }

        void AppState_ScriptCommand(object sender, string command)
        {
            if (command == ScriptCommands.UpdateLayers)
            {
                Execute.OnUIThread(UpdateLayers);
            }
        }

        public void ToggleOpacity(sLayer layer)
        {
            if (layer == null) return;
            layer.Layer.Opacity = (layer.Layer.Opacity > 0.1) ? 0 : 1;
        }


        public void AddLayer()
        {
            if (Element != null) Element.Flip();
        }

        private void sbSelect_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //Layers = (GroupLayer)((SurfaceButton)sender).DataContext;
        }

        private void sbZoom_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var l = (Layer)((SurfaceButton)sender).DataContext;
            AppState.ViewDef.MapControl.ZoomTo(l.FullExtent);
        }

        public void Start(IStartStopLayer layer)
        {
            if (layer == null) return;

            layer.Start();
        }

        public void Stop(IStartStopLayer layer)
        {
            if (layer == null) return;
            layer.Stop();
        }

        public string Name
        {
            get { return "Layers"; }
        }

        public void Start()
        {

        }

        private void dirtyTimer_Tick(object sender, EventArgs e)
        {
            if (!dirty) return;
            dirty = false;
            UpdateLayers();
        }

        public void Pause()
        {
            dirtyTimer.Stop();
        }

        public void Stop()
        {
            dirtyTimer.Stop();
        }
    }

    public class LayerBackgroundColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = (bool)value;
            return (s)
                ? new SolidColorBrush(new Color() { A = 255, R = 0, G = 0, B = 0 })
                : new SolidColorBrush(new Color() { A = 125, R = 0, G = 0, B = 0 });
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class CanZoomConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var layer = value as Layer;
            if (layer != null) return (layer.FullExtent == null) ? Visibility.Collapsed : Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
