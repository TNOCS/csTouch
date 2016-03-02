using System.Windows.Threading;
using Caliburn.Micro;
using csCommon;
using csCommon.Types.DataServer.PoI;
using csDataServerPlugin.Extensions;
using csEvents;
using csEvents.Sensors;
using csShared;
using csShared.Controls.Popups.MapCallOut;
using csShared.FloatingElements;
using csShared.Geo;
using csShared.Interfaces;
using csShared.TabItems;
using csShared.Utils;
using DataServer;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Zandmotor.Controls.Plot;
using Directory = System.IO.Directory;
using Point = System.Windows.Point;

namespace csDataServerPlugin
{
    using csCommon.Utils;

    [Export(typeof(IPlugin))]
    public class DataServerPlugin : PropertyChangedBase, IPlugin
    {
        public readonly DataServerBase Dsb = new DataServerBase { SyncPriority = 5 };
        private bool hideFromSettings;
        private bool isRunning;
        private BindableCollection<dsLayer> layers = new BindableCollection<dsLayer>();

        private IPluginScreen screen;
        private ISettingsScreen settings;

        public FloatingElement Element { get; set; }

        public EventList EventList { get; set; }

        private List<string> autoStartServices = new List<string>();

        public List<string> AutoStartServices
        {
            get { return autoStartServices; }
            set { autoStartServices = value; }
        }

        public Dictionary<string, Type> Models { get; set; }

        public BindableCollection<dsLayer> Layers
        {
            get { return layers; }
            set { layers = value; }
        }

        private BindableCollection<dsStaticLayer> staticLayers = new BindableCollection<dsStaticLayer>();

        public BindableCollection<dsStaticLayer> StaticLayers
        {
            get { return staticLayers; }
            set { staticLayers = value; NotifyOfPropertyChange(() => StaticLayers); }
        }



        public IPluginScreen Screen
        {
            get { return screen; }
            set
            {
                screen = value;
                NotifyOfPropertyChange(() => Screen);
            }
        }

        public bool CanStop
        {
            get { return false; }
        }

        public ISettingsScreen Settings
        {
            get { return settings; }
            set
            {
                settings = value;
                NotifyOfPropertyChange(() => Settings);
            }
        }

        public bool HideFromSettings
        {
            get { return hideFromSettings; }
            set
            {
                hideFromSettings = value;
                NotifyOfPropertyChange(() => HideFromSettings);
            }
        }

        public int Priority
        {
            get { return 3; }
        }

        public string Icon
        {
            get { return @"/csCommmon;component/Resources/Icons/dataserver.png"; }
        }

        public string Name
        {
            get { return "DataServerPlugin"; }
        }

        public bool IsRunning
        {
            get { return isRunning; }
            set
            {
                isRunning = value;
                NotifyOfPropertyChange(() => IsRunning);
            }
        }

        public AppStateSettings AppState { get; set; }

        public void Init()
        {
            Screen = new DataServerViewModel();

            AppState.ViewDef.FolderIcons[@"Layers\Shared Layers"] = "pack://application:,,,/csCommon;component/Resources/Icons/Person.png";

        }

        

        private void ServicesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    //TODO: remove layer
                    //foreach (var s in StaticLayers) {}
                    break;

                case NotifyCollectionChangedAction.Add:
                    // find out what the AutoStart services are
                    var ass = AppState.Config.Get("Services.AutoStart", "");
                    if (!string.IsNullOrEmpty(ass))
                    {
                        AutoStartServices = ass.Split(',').ToList();
                    }
                    foreach (var s in e.NewItems)
                    {
                        var ps = s as PoiService;
                        if (ps == null) continue;
                        dsBaseLayer newLayer;

                        if (ps.StaticService)
                        {
                            newLayer = new dsStaticLayer(ps, this) { Visible = true };
                            if (string.IsNullOrEmpty(ps.RelativeFolder))
                            {
                                AppState.ViewDef.AcceleratedLayers.ChildLayers.Add(newLayer);
                                newLayer.Parent = AppState.ViewDef.AcceleratedLayers;
                            }
                            else
                            {
                                var folder = AppState.ViewDef.FindOrCreateAcceleratedGroupLayer(ps.RelativeFolder.Trim('\\').Replace('\\', '/'));
                                folder.ChildLayers.Add(newLayer);
                                newLayer.Parent = folder;
                            }
                            StaticLayers.Add((dsStaticLayer)newLayer);
                            ps.Layer = newLayer;
                            newLayer.Initialize();
                            if (ps.AutoStart) 
                                ((dsStaticLayer)newLayer).Start();
                        }
                        else
                        {
                            newLayer = new dsLayer(ps, this) { Visible = true };
                            if (string.IsNullOrEmpty(ps.RelativeFolder))
                            {
                                AppState.ViewDef.Layers.ChildLayers.Add(newLayer);
                                newLayer.Parent = AppState.ViewDef.Layers;
                            }
                            else
                            {
                                var folder = AppState.ViewDef.FindOrCreateGroupLayer(ps.RelativeFolder.Trim('\\').Replace('\\', '/'));
                                folder.ChildLayers.Add(newLayer);
                                newLayer.Parent = folder;
                            }
                            Layers.Add((dsLayer)newLayer);
                            ps.Layer = newLayer;
                            newLayer.Initialize();
                            var myLayer = newLayer as dsLayer;
                            if (ps.AutoStart && !myLayer.IsStarted)
                                myLayer.Start();
                        }


                        #region old

                        //if (!ps.StaticService)
                        //{
                        //    if (AutoStartServices.Contains(ps.Name) )
                        //    {
                        //        this.Dsb.Subscribe(ps, Mode.server);
                        //    }
                        //    if (ps.AutoStart)
                        //    {
                        //        this.Dsb.Subscribe(ps, Mode.client);
                        //    }
                        //    AddDataService(ps);
                        //}
                        //else
                        //{
                        //    var sl = new dsStaticLayer(ps, this) { Visible = true };

                        //    ps.Layer = sl;
                        //    if (string.IsNullOrEmpty(ps.RelativeFolder))
                        //    {
                        //        AppState.ViewDef.AcceleratedLayers.ChildLayers.Add(sl);
                        //    }
                        //    else
                        //    {
                        //        var folder = AppState.ViewDef.FindOrCreateAcceleratedGroupLayer(ps.RelativeFolder.Trim('\\').Replace('\\','/'));
                        //        folder.ChildLayers.Add(sl);
                        //    }


                        //    sl.Initialize();
                        //    StaticLayers.Add(sl);
                        //    if (AutoStartServices.Contains(ps.Name))
                        //    {
                        //        Dsb.Subscribe(ps);
                        //    }
                        //    if (ps.AutoStart) sl.Start();
                        //}
                        #endregion
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (var s in e.OldItems)
                    {
                        var ps = s as PoiService;
                        if (ps == null || !ps.StaticService) continue;
                        var l = StaticLayers.FirstOrDefault(k => k.Service == ps);
                        if (l != null) StaticLayers.Remove(l);
                    }
                    break;
            }
        }

        public void SetDashboard(ref DashboardState dashboard)
        {
            var res = "";
            foreach (var service in Dsb.Services.Where(k => k is PoiService && k.IsFileBased))
            {
                var s = (PoiService)service;
                res += s.RelativeFolder + "|" + s.Id + "|" + s.Name + "|" + s.StaticService + "|" + s.IsSubscribed + "|";
                if (s.Layer != null)
                {
                    var l = s.Layer;
                    res += l.Visible + "|" + l.Opacity.ToString(CultureInfo.InvariantCulture) + "|";
                    var startStopLayer = s.Layer as IStartStopLayer;
                    if (startStopLayer != null) res += startStopLayer.IsStarted + "|";

                    if (s.Layer.ChildLayers.Any())
                    {
                       
                        foreach (var sl in s.Layer.ChildLayers)
                        {
                            res += sl.ID + "_" + sl.Visible + "_" + sl.Opacity.ToString(CultureInfo.InvariantCulture) + ";";

                        }
                                           }

                }
                res += "~";
            }
            dashboard.States["Layers"] = res;
        }

        private void Dashboards_DashboardActivated(object sender, DashboardEventArgs e)
        {
            if (e.Dashboard == null) return;
            var s = e.Dashboard.States;

            if (!s.ContainsKey("Layers")) return;
            var layerState = s["Layers"].Split('~');
            foreach (var l in layerState)
            {
                try
                {
                    var ll = l.Split('|');
                    if (ll.Length < 4) continue;
                    var id = Guid.Parse(ll[1]);

                    var service = Dsb.Services.FirstOrDefault(k => k.Id == id);
                    var poiService = service as PoiService;
                    if (poiService == null) continue;
                    var ps = poiService;
                    bool started;
                    if (!bool.TryParse(ll[7], out started)) continue;
                    if (!ps.Layer.IsStarted && started) ((IStartStopLayer)ps.Layer).Start();
                    if (ps.Layer.IsStarted && !started)
                        ((IStartStopLayer)ps.Layer).Stop();

                    if (ll.Length > 7)
                    {
                        var sl = ll[8];
                        foreach (var ls in sl.Split(';'))
                        {
                            if (!string.IsNullOrEmpty(ls))
                            {
                                var sll = ls.Split('_');
                                if (sll.Length == 3)
                                {
                                    var a = ps.Layer.ChildLayers.FirstOrDefault(k => k.ID == sll[0]);
                                    if (a!=null)
                                    {
                                        a.Visible = bool.Parse(sll[1]);
                                        a.Opacity = double.Parse(sll[2], CultureInfo.InvariantCulture);
                                    }
                                }
                            }
                        }
                        
                    }
                    //ps.Initialized += (ts, b) =>
                    //{
                    //    if (ll.Length > 7)
                    //    {

                    //        if (ps.Layer is dsBaseLayer)
                    //        {
                    //            var bl = (dsBaseLayer) ps.Layer;
                    //            Execute.OnUIThread(() =>
                    //            {

                    //                bl.Visible = bool.Parse(ll[5]);
                    //                bl.Opacity = double.Parse(ll[6], CultureInfo.InvariantCulture);
                    //            });


                    //            if (bool.TryParse(ll[7], out started))
                    //            {
                    //                if (started) ((IStartStopLayer) bl).Start();
                    //                if ((((IStartStopLayer) bl).IsStarted) && !started)
                    //                    ((IStartStopLayer) bl).Stop();
                    //            }


                    //        }

                    //    }
                    //};
                }
                catch (Exception et)
                {
                    Logger.Log("Dashboard", "Error restoring layer", et.Message, Logger.Level.Error);
                }
            }
            AppState.ViewDef.UpdateLayers();
            //if (s.ContainsKey(TimelineState)) TimelineVisible = bool.Parse(s[TimelineState]);
        }

        private string layerFolder;
        private string shapeFolder;

        public void Start()
        {
            Models = new Dictionary<string, Type>();
            AppState.DataServer = Dsb;

            //Dsb.InitImb();
            Dsb.client = AppState.Imb;
            Dsb.UnSubscribed += DsbUnSubscribed;
            Dsb.Services.CollectionChanged += ServicesCollectionChanged;
            AppState.DashboardStateStates.DashboardActivated += Dashboards_DashboardActivated;
            AppState.DashboardStateStates.RegisterHandler(SetDashboard);

            // add layers
            
            layerFolder = Path.Combine(Directory.GetCurrentDirectory(),AppState.Config.Get("Poi.LocalFolder", "PoiLayers"));
            Dsb.Start(layerFolder, Mode.client, layerFolder);

            InitShapeLayer();
            //s = new StartPanelTabItem
            //{
            //    Name = "PoIs",
            //    HeaderStyle = TabHeaderStyle.Image,
            //    Image = new BitmapImage(new Uri("pack://application:,,,/csCommon;component/Resources/Icons/position.png")),
            //    ModelInstance = new DataServicesSelectionViewModel { Plugin = this }
            //};
            //AppState.AddStartPanelTabItem(s);
            
            IsRunning = true;
            EventList = new EventList{ Name = "Data server plugin"};
            AppState.EventLists.AddEventList(EventList);

            //foreach (var st in Dsb.Services.Where(k => k.IsSubscribed))
            //{
            //    AddDataService((PoiService)st);
            //}

            AppState.DataServer = Dsb;
        }
        

        private void InitShapeLayer()
        {
            // find folder, check if exists
            shapeFolder = AppState.Config.Get("Poi.ShapeFolder", "ShapeLayers");
            if (shapeFolder[shapeFolder.Length - 1] != '\\') shapeFolder += @"\";
            shapeFolder = Environment.ExpandEnvironmentVariables(shapeFolder);
            if (Directory.Exists(shapeFolder)) FindShapeLayers(shapeFolder); // Directory.CreateDirectory(shapeFolder);
        }

        private void FindShapeLayers(string folder)
        {
            var dir = new DirectoryInfo(folder);
            var files = dir.GetFiles("*.shp");
            foreach (var f in files.OrderByDescending(f => f.Name))
            {
                ProcessShapeFile(f.FullName, folder);
            }

            var dirs = dir.GetDirectories();
            foreach (var d in dirs.OrderByDescending(d => d.Name)) FindShapeLayers(d.FullName);
        }

        private void ProcessShapeFile(string file, string folder)
        {
            var dir        = new DirectoryInfo(shapeFolder);
            var fi         = new FileInfo(file);
            var service    = ShapeService.CreateShapeService(fi.GetShapeFileBaseName(), Guid.NewGuid(), folder, folder.Replace(dir.FullName, ""));
            service.File   = file;
            service.Folder = folder;
        }

        // SdJ added this new method.
        public ShapeService CreateShapeService(string file, string folder, string relativeFolder = null)
        {
            if (relativeFolder == null && string.IsNullOrEmpty(shapeFolder))
            {
                InitShapeLayer(); // Sets the shape folder if needed.
                var dir = new DirectoryInfo(shapeFolder);
                relativeFolder = folder.Replace(dir.FullName, "");
            }            
            var fi = new FileInfo(file);
            var service = ShapeService.CreateShapeService(fi.GetShapeFileBaseName(), Guid.NewGuid(), folder, relativeFolder);
            service.File = file;
            service.Folder = folder;
            return service;
        }

        public void Pause()
        {
            IsRunning = false;
        }



        public void Stop()
        {
            //AppState.RemoveStartPanelTabItem(s);
            Dsb.UnSubscribed -= DsbUnSubscribed;
            Dsb.Stop();
            IsRunning = false;
            AppState.DashboardStateStates.DashboardActivated -= Dashboards_DashboardActivated;
            AppState.DashboardStateStates.RemoveHandler(SetDashboard);
        }



     

        private void DsbUnSubscribed(object sender, ServiceSubscribeEventArgs e)
        {
            Execute.OnUIThread(() =>
            {
                var service = e.Service as PoiService;
                if (service != null)
                {
                    service.TimeLine.Clear();
                }
                if (!e.Service.StaticService) return;
                var layer = StaticLayers.FirstOrDefault(k => k.Service.Id == e.Service.Id);
                e.Service.Reset();

                if (layer != null)
                    layer.End();
            });
        }

        private void AddDataService(PoiService s)
        {
            s.Events.CollectionChanged += EventsCollectionChanged;
            //s.PoIs.CollectionChanged += PoIs_CollectionChanged;
            Execute.OnUIThread(() =>
            {
                if (s.StaticService) return;
                //var layer = new dsLayer(s, this);
                //s.Layer = layer;
                //s.SetExtent(AppState.ViewDef.WorldExtent);
                //Layers.Add(layer);
                var layer = (dsLayer)s.Layer;
                s.PropertyChanged += (t, f) =>
                {
                    if (f.PropertyName == "IsInitialized")
                        UpdateLayerTabs(s, (dsLayer)s.Layer);
                };
                if (s.IsInitialized && s.Settings != null)
                {
                    s.Settings.PropertyChanged += (t, f) => UpdateLayerTabs(s, layer);
                }

                layer.PropertyChanged += (e, sf) =>
                {
                    if (sf.PropertyName == "DetailTabVisible")
                    {
                        UpdateLayerTabs(s, layer);
                    }
                };

                UpdateLayerTabs(s, layer);
                if (string.IsNullOrEmpty(s.RelativeFolder))
                {
                    AppState.ViewDef.Layers.ChildLayers.Add(layer);
                }
                else
                {
                    var folder = AppState.ViewDef.FindOrCreateGroupLayer(s.RelativeFolder.Trim('\\').Replace('\\', '/'));
                    folder.ChildLayers.Add(layer);
                }
                if (AppState.ViewDef.Layers != null)
                    layer.OpenTab();
                layer.IsTabActive = true;
                //layer.Initialize();
                if (s.IsInitialized) UpdateLayerTabs(layer.Service, layer);
                s.PingReceived += s_PingReceived;
            });
        }

        private void s_PingReceived(Service s, BaseContent b)
        {
            var wm = new WebMercator();
            var pos = (MapPoint)wm.FromGeographic(new MapPoint(b.Position.Longitude, b.Position.Latitude));
            var pcov = new PingCallOutViewModel { Ping = b };
            var callOut = new MapCallOutViewModel
            {
                Width = 250,
                TimeOut = new TimeSpan(0, 0, 0, 5),
                CanClose = false,
                ForegroundBrush = Brushes.White,
                BackgroundBrush = Brushes.Red,
                ViewModel = pcov,
                Point = pos
            };
            AppState.Popups.Add(callOut);
            //AppState.TriggerNotification();
            AppState.ViewDef.ZoomAndPoint(new Point(b.Position.Longitude, b.Position.Latitude));
        }


        private void EventsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Execute.OnUIThread(() =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (Event ni in e.NewItems)
                        {
                            if (true)
                            {
                                var eb = new EventBase { Date = ni.Date, Name = ni.TimelineString };
                                if (ni.Position != null)
                                {
                                    eb.Latitude = ni.Position.Latitude;
                                    eb.Longitude = ni.Position.Longitude;
                                }
                                
                                eb.AlwaysShow = true;
                                eb.Id = ni.Id;
                                EventList.Add(eb);
                            }
                        }
                        break;
                }
            });
        }

        public void UpdateLayerTabs(PoiService service, dsLayer layer)
        {
            var tabVisible = true;
            if (service.Settings != null)
            {
                tabVisible = service.Settings.TabBarVisible;
            }
            // check if it already exists

            Execute.OnUIThread(() =>
            {
                var inst = AppState.StartPanelTabItems.FirstOrDefault(
                    k => k.ModelInstance is TabItemViewModel &&
                    ((TabItemViewModel)k.ModelInstance).Service == service);

                if (inst == null && service.IsInitialized && tabVisible)
                {
                    var ptvm = new TabItemViewModel { Plugin = this, Service = service, Layer = layer };
                    var s = new StartPanelTabItem { Name = service.Name };
                    ptvm.TabItem = s;
                    if (!service.IsLocal || service.Mode == Mode.server)
                    {
                        s.SupportImage =
                            new BitmapImage(
                                new Uri("pack://application:,,,/csCommon;component/Resources/Icons/online.png"));
                    }
                    s.ModelInstance = ptvm;
                    AppState.AddStartPanelTabItem(s);
                }
                else if ((!service.IsInitialized || !tabVisible) && inst != null)
                {
                    AppState.RemoveStartPanelTabItem(inst);
                }
            });
        }

        public void GoTo()
        {
        }
    }

    //namespace DataServer
    //{
    //    public partial class PoiService
    //    {
    //        public GraphicsLayer Layer { get; set; }
    //    }
    //}
}