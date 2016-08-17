using Caliburn.Micro;
using csCommon.Logging;
//using csCommon.Plugins.DataServerPlugin.Utils;
using csShared;
using csShared.Controls.Popups.InputPopup;
using csShared.Controls.Popups.MenuPopup;
using csShared.FloatingElements;
using csShared.Geo;
using DataServer;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Action = System.Action;
using Task = System.Threading.Tasks.Task;

namespace csDataServerPlugin
{
    [DebuggerDisplay("Layer {ID}, {PoiLayers.Count} sub layers, {Children.Count} children")]
    public class dsLayer : dsBaseLayer, INotifyPropertyChanged, IOnlineLayer, IStartStopLayer, IServiceLayer, IMenuLayer, ILayerWithMoreChildren, ITabLayer
    {
        private readonly WebMercator mercator = new WebMercator();

        private readonly Dictionary<Guid, Graphic> tracks = new Dictionary<Guid, Graphic>();
        private bool detailTabVisible = true;

        private bool reviewMode;

        //private GraphicsLayer eventLayer;

        private bool showHistory;

        private ConcurrentBag<PoI> batch = new ConcurrentBag<PoI>();

        //private ElementLayer groundOverlayLayer;

        public bool IsOnline
        {
            get { return (service != null && service.Mode == Mode.server); }
        }

        public bool IsShared
        {
            get { return (service != null && service.IsShared && !IsOnline); }
        }

        public dsLayer(PoiService s, DataServerPlugin p)
        {
            if (s == null)
            {
                csCommon.Logging.LogCs.LogMessage(String.Format("dsLayer: Create PoiService '{0}'", "empty"));
                s = new PoiService();
            }
            Service = s;
            ID = Service.Name;
            plugin = p;
            PoiLayers = new Dictionary<string, dsPoiLayer>();
            OverlayLayers = new Dictionary<string, dsOverlayLayer>();
            Children = new ObservableCollection<Layer>();
            s.PropertyChanged += s_PropertyChanged;
        }

        void s_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Mode") OnPropertyChanged("IsOnline");
        }

        void SVisibilityChanged(object sender, EventArgs e)
        {
            foreach (var pl in PoiLayers) pl.Value.Visible = Service.IsVisible;
            Visible = Service.IsVisible;
            Opacity = (Visible) ? 1.0 : 0.0;
        }

        public Dictionary<string, dsPoiLayer> PoiLayers { get; set; }
        public Dictionary<string, dsOverlayLayer> OverlayLayers { get; set; }

        public bool ReviewMode
        {
            get { return reviewMode; }
            set
            {
                reviewMode = value;
                OnPropertyChanged("ReviewMode");
            }
        }

        public bool ShowHistory
        {
            get { return showHistory; }
            set
            {
                showHistory = value;
                OnPropertyChanged("ShowHistory");
            }
        }

        public bool DetailTabVisible
        {
            get { return detailTabVisible; }
            set
            {
                detailTabVisible = value;
                OnPropertyChanged("DetailTabVisible");
            }
        }

        public Guid Id
        {
            get { return service.Id; }
        }

        public ObservableCollection<Layer> Children { get; set; }
        public new event PropertyChangedEventHandler PropertyChanged;
        //private BitmapSource voiceImage;

        private bool zoomVisible;

        public bool ZoomVisible
        {
            get { return zoomVisible; }
            set { zoomVisible = value; OnPropertyChanged("ZoomVisible"); }
        }

        public override void Initialize()
        {
            base.Initialize();

            Visible = false;
            SpatialReference = new SpatialReference(4326);
            Service.AllPoisRefreshed += Service_AllPoisRefreshed;
        }

        void Service_AllPoisRefreshed(object sender, EventArgs e)
        {
            RefreshAllVisiblePois(true);
        }

        void MapControlExtentChanged(object sender, ExtentEventArgs e)
        {
            Service.SetExtent(AppState.ViewDef.WorldExtent);
        }

        void ServiceAllPoisRefreshed(object sender, EventArgs e)
        {
            RefreshAllVisiblePois();
        }

        private void RefreshAllVisiblePois()
        {
            var changed = false;
            var vp = Service.ContentInExtent;
            Parallel.ForEach(vp, p =>
            {
                if (p.UpdateAnalysisStyle()) changed = true;
            });
            if (changed)
            {
                Execute.OnUIThread(() =>
                {
                    foreach (var p in vp.Cast<PoI>())
                    {
                        p.TriggerUpdated();
                    }
                });
            }
        }

        public IdsChildLayer FindPoiLayer(PoI p)
        {
            if (p.Data.ContainsKey("layer")) return (IdsChildLayer)p.Data["layer"];
            //string name = (p.Labels.ContainsKey("PoiLayer")) ? p.Labels["PoiLayer"] : "pois";
            if (p.NEffectiveStyle.DrawingMode.HasValue &&
                p.NEffectiveStyle.DrawingMode.Value == DrawingModes.ImageOverlay)
            {
                var name = (string.IsNullOrEmpty(p.Layer)) ? Service.Name : p.Layer;
                if (OverlayLayers.ContainsKey(name)) return OverlayLayers[name];
                var npl = new dsOverlayLayer(this) { ID = name };

                OverlayLayers[name] = npl;
                ChildLayers.Add(npl);

                CheckLayerOrder();

                npl.Initialize();
                return npl;
            }
            else
            {
                var name = (string.IsNullOrEmpty(p.Layer)) ? Service.Name : p.Layer;
                if (PoiLayers.ContainsKey(name)) return PoiLayers[name];
                var npl = new dsPoiLayer(this) { ID = name };

                if (UseClusterer || (p.NEffectiveStyle.Cluster.HasValue && p.NEffectiveStyle.Cluster.Value))
                {
                    npl.Clusterer = new FlareClusterer();
                }

                PoiLayers[name] = npl;
                ChildLayers.Add(npl);

                CheckLayerOrder();

                npl.Initialize();
                return npl;
            }
        }

        /// <summary>
        /// Add new poi
        /// </summary>
        /// <param name="p"></param>
        public new void AddPoi(PoI p)
        {
            if (p == null) return;
            var poiLayer = FindPoiLayer(p);

            //AddLabels(p);
            p.UpdateAnalysisStyle();

            poiLayer.AddPoi(p);

            // check models
            p.FindModels();
            base.AddPoi(p);
        }

        /// <summary>
        /// remove poi
        /// </summary>
        /// <param name="p"></param>
        public new void RemovePoi(PoI p)
        {
            LogCs.LogMessage(String.Format("dsLayer: RemovePoi {0} ({1})", p.Id, p.Name ?? ""));
            base.RemovePoi(p);
            if (!p.Data.ContainsKey("layer")) return;
            var gl = p.Data["layer"] as IdsChildLayer;
            if (gl == null) return;

            if (p.Data.ContainsKey("FloatingElement"))
            {
                var fe = (FloatingElement)p.Data["FloatingElement"];
                if (fe != null) AppState.FloatingItems.RemoveFloatingElement(fe);
                p.Data.Remove("FloatingElement");
            }
            gl.RemovePoi(p);
        }

        private static void TimelineManagerTimeChanged(object sender, EventArgs e)
        {
            // EV I've uncommented this as it is already called in dsBaseLayer.TimelineManagerFocusTimeChanged
            //ThreadPool.QueueUserWorkItem(delegate
            //{
            //    Parallel.ForEach(service.PoIs, p =>
            //    {
            //        if (p.Sensors == null || !p.Sensors.Any()) return;
            //        foreach (var s in p.Sensors.AsParallel())
            //        {
            //            s.Value.SetFocusDate(AppState.TimelineManager.FocusTime);
            //        }
            //        //if (p.NEffectiveStyle.TimelineBehaviour.Value != TimelineBehaviours.None)
            //        //{

            //        //}
            //    });

            //    //RefreshAllVisiblePois();
            //});
        }

        protected new void OnPropertyChanged(string name)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(name));
        }

        private void ServicePoiUpdated(object sender, PoiUpdatedEventArgs e)
        {
            UpdatePoi((PoI)e.Poi);
        }

        private void PoisCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        foreach (var p in Service.PoIs.Cast<PoI>())
                        {
                            RemovePoi(p);
                        }
                    }));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        foreach (PoI p in e.OldItems)
                            RemovePoi(p);
                    }));
                    break;
                case NotifyCollectionChangedAction.Add:
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        foreach (PoI p in e.NewItems)
                        {
                            if (Service.PoIs.IsBatchLoading > 0)
                            {
                                batch.Add(p);
                            }
                            else
                            {
                                AddPoi(p);
                                //PoI p1 = p;
                            }
                        }
                    }));
                    break;
            }
        }

        private async void UpdateAllPois()
        {
            //var sw = new Stopwatch();
            //sw.Start();
            Service.PoIs.StartBatch();
            foreach (var baseContent in Service.PoIs)
            {
                var p = (PoI)baseContent;
                AddPoi(p);
            }
            Service.PoIs.FinishBatch();

            //sw.Stop();
            if (Service.PoIs.Any())
                await Task.Run(() => Service.ReadSensorFile());
        }

        public void UpdatePoi(PoI p)
        {
            if (Service.PoIs.Contains(p))
            {
                //RemovePoi(p);
                //AddPoi(p);
                var poiLayer = FindPoiLayer(p);
                poiLayer.UpdatePoi(p);
            }
            else if (Service.PoITypes.Contains(p))
            {
                foreach (var baseContent in Service.PoIs.Where(k => k.PoiTypeId == p.ContentId)) {
                    var pt = (PoI) baseContent;
                    UpdatePoi(pt);
                }
            }
        }

        public void OpenPoiPopup(BaseContent poI)
        {
            if (poI.Data.ContainsKey("FloatingElement") && AppState.FloatingItems.Contains((FloatingElement)poI.Data["FloatingElement"]))
                return;
            var pp = new PoiPopupViewModel { PoI = poI, Layer = this, Service = poI.Service as PoiService };
            var s = new Size(300, 500);
            var kmlpoint = new KmlPoint(poI.Position.Longitude, poI.Position.Latitude);
            var p = AppState.ViewDef.MapPoint(kmlpoint);

            if (p.X <= AppState.MainBorder.ActualWidth / 2)
                p.X += s.Width / 2 + 25;
            else
                p.X -= s.Width / 2 + 25;

            var fe = FloatingHelpers.CreateFloatingElement(poI.Name, p, s, pp);
            fe.AssociatedPoint = kmlpoint;
            poI.Data["FloatingElement"] = fe;

            AppState.FloatingItems.AddFloatingElement(fe);
        }

        public new void Stop()
        {
            base.Stop();

            CloseTab();
            plugin.Dsb.UnSubscribe(Service);
            IsStarted = false;
            Service.Unsubscribe();
            Service.IsSubscribed = false;
            foreach (var baseContent in Service.PoIs)
            {
                var p = (PoI)baseContent;
                RemovePoi(p);
            }
            Service.PoIs.CollectionChanged -= PoisCollectionChanged;
            Service.AllPoisRefreshed -= ServiceAllPoisRefreshed;
            if (AppState.ViewDef.MapControl != null) AppState.ViewDef.MapControl.ExtentChanged -= MapControlExtentChanged;
            Service.PoiUpdated -= ServicePoiUpdated;
            Service.VisibilityChanged -= SVisibilityChanged;
            foreach (var layer in ChildLayers.OfType<GraphicsLayer>())
            {
                layer.ClearGraphics();
            }

            foreach (var layer in Children.OfType<GraphicsLayer>())
            {
                layer.ClearGraphics();
            }
            //Children.Clear();
            //ChildLayers.Clear();
            tracks.Clear();
            if (EventList != null) EventList.Clear();
            AppState.EventLists.Remove(EventList);

            plugin.UpdateLayerTabs(service, this);
            AppState.ViewDef.UpdateLayers();
        }

        private Guid dguid;
        private bool autoShare;

        public new void Start(bool share = false)
        {
            autoShare = share;
            dguid = AppState.AddDownload("Start Layer", "");

            base.Start();
            Init();
            Service.SetExtent(AppState.ViewDef.WorldExtent);
            Service.Initialized -= ServiceInitialized;
            Service.Initialized += ServiceInitialized;
            Service.PoiUpdated  += ServicePoiUpdated;

            plugin.Dsb.Subscribe(Service);
        }

        private void ServiceInitialized(object sender, EventArgs e) {
            Execute.OnUIThread(() => {
                UpdateAllPois();
                service.CheckOriginalBackup();
                TriggerStarted();

                Execute.OnUIThread(async () => {
                    if (Service.Settings != null && Service.Settings.OpenTab) {
                        OpenTab();
                        await Service.DoSearch();
                    }
                    Visible = true;
                    IsLoading = false;
                });

                AppState.FinishDownload(dguid);
                //UpdateAllPois();

                AppState.TimelineManager.TimeChanged -= TimelineManagerTimeChanged;
                AppState.TimelineManager.TimeChanged += TimelineManagerTimeChanged;
                AppState.ViewDef.MapControl.ExtentChanged -= MapControlExtentChanged;
                AppState.ViewDef.MapControl.ExtentChanged += MapControlExtentChanged;

                if (autoShare) service.MakeOnline();
                plugin.UpdateLayerTabs(service, this);
            });

            Service.PoIs.CollectionChanged -= PoisCollectionChanged;
            Service.PoIs.CollectionChanged += PoisCollectionChanged;
            Service.PoIs.BatchStarted -= PoIs_BatchStarted;
            Service.PoIs.BatchStarted += PoIs_BatchStarted;
            Service.PoIs.BatchFinished -= PoIs_BatchFinished;
            Service.PoIs.BatchFinished += PoIs_BatchFinished;
        }

        public new List<System.Windows.Controls.MenuItem> GetMenuItems()
        {
            var l = base.GetMenuItems();

            if (service.IsSubscribed)
            {
                if (service.IsLocal)
                {
                    if (service.Mode == Mode.client)
                    {
                        var shareonline = MenuHelpers.CreateMenuItem("Start Sharing", MenuHelpers.OnlineIcon);
                        shareonline.Click += (e, f) => service.MakeOnline();
                        l.Add(shareonline);

                        if (AppState.Imb.ActiveGroup != null)
                        {
                            var sharegroup = MenuHelpers.CreateMenuItem("Share with group", MenuHelpers.OnlineIcon);
                            sharegroup.Click += (e, f) => service.ShareInGroup();
                            l.Add(sharegroup);
                        }
                    }
                    else
                    {
                        var makelocal = MenuHelpers.CreateMenuItem("Stop Sharing", MenuHelpers.OnlineIcon, Brushes.Gray);
                        makelocal.Click += (e, f) => service.MakeLocal();
                        l.Add(makelocal);
                    }
                }

                if (service.IsLocal && service.Settings != null && service.Settings.CanEdit)
                {
                    var rename = MenuHelpers.CreateMenuItem("Rename", MenuHelpers.TextIcon);
                    rename.Click += (e, f) => RenameService(rename);
                    l.Add(rename);
                }
            }
            else if (service.IsLocal)
            {
                var startshare = MenuHelpers.CreateMenuItem("Start & Share", MenuHelpers.OnlineIcon);
                startshare.Click += (e, f) => StartShare();
                l.Add(startshare);
            }

            var delete = MenuHelpers.CreateMenuItem("Delete", MenuHelpers.DeleteIcon);
            delete.Click += (e, f) => DeleteService();

            l.Add(delete);

            return l;
        }

        public void StartShare()
        {
            Start(true);
        }

        public void RenameService(FrameworkElement sb)
        {
            var input = new InputPopupViewModel
            {

                RelativePosition = sb.PointToScreen(new Point(0, 0)),
                TimeOut = new TimeSpan(0, 0, 2, 5),
                VerticalAlignment = VerticalAlignment.Bottom,
                Title = "Service Name",
                Width = 250.0,
                DefaultValue = service.Name
            };
            input.Saved += (st, ea) =>
            {
                var oldName = service.FileName;
                var old = service.Name;
                service.Name = ea.Result;
                if (oldName == service.FileName) return;
                if (File.Exists(oldName) && (!File.Exists(service.FileName)))
                {
                    if (service.SaveXml())
                    {
                        File.Delete(oldName);
                        AppStateSettings.Instance.RenameStartPanelTabItem(old, service.Name);
                    }
                    else
                        service.Name = old;
                }
                else
                    service.Name = old;
                plugin.UpdateLayerTabs(service, this);
            };
            AppStateSettings.Instance.Popups.Add(input);
        }

        private void DeleteService()
        {
            var nea = new NotificationEventArgs
            {
                Text = "Are you sure?",
                Header = "Delete " + service.Name,
                Duration = new TimeSpan(0, 0, 45),
                Background = Brushes.Red,
                Image =
                    new BitmapImage(new Uri(@"pack://application:,,,/csCommon;component/Resources/Icons/Delete.png")),
                Foreground = Brushes.White,
                Options = new List<string> { "Yes", "No" }
            };
            nea.OptionClicked += (ts, n) =>
            {
                if (n.Option != "Yes") return;
                if (service.IsSubscribed && service.Mode == Mode.server) service.MakeLocal();
                if (IsStarted) Stop();
                AppState.DataServer.DeleteService(service);
                plugin.UpdateLayerTabs(service, this);
            };
            AppStateSettings.Instance.TriggerNotification(nea);
        }

        void PoIs_BatchFinished(object sender, EventArgs e)
        {
            Execute.OnUIThread(() =>
            {
                var all = batch.Take(batch.Count).ToArray();
                for (var i = 0; i < all.Count(); i++) AddPoi(all[i]);
            });
        }

        void PoIs_BatchStarted(object sender, EventArgs e)
        {
            batch = new ConcurrentBag<PoI>();
        }
    }
}