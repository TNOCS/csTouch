using System.Windows;
using System.Windows.Navigation;
using Caliburn.Micro;
using csCommon.Types.DataServer.PoI;
using csCommon.Views.Dialogs;
using csShared.Controls.Popups.InputPopup;
using csEvents;
using csShared;
using csShared.Controls.Popups.MenuPopup;
using csShared.Controls.SlideTab;
using csShared.Geo;
using csShared.TabItems;
using csShared.Timeline;
using csShared.Utils;
using DataServer;
using ESRI.ArcGIS.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using nl.tno.cs.presenter;

namespace csDataServerPlugin
{
    using System.Diagnostics;

    public interface ILoadingLayer
    {
        bool  IsLoading { get; }
        event EventHandler StartedLoading;
        event EventHandler StoppedLoading;
    }

    public class dsBaseLayer : GroupLayer, IStartStopLayer, IMenuLayer, ILoadingLayer
    {
        //private readonly object refreshLock = new object();

        public event EventHandler StartedLoading;
        public event EventHandler StoppedLoading;

        private StartPanelTabItem tabItem;
        private bool              canStart;
        private bool              canStop;
        private bool              isStarted;
        private string            state;

        public PoiService       service;
        public DataServerPlugin plugin       { get; set; }
        public EventList        EventList    { get; set; }
        public bool             UseClusterer { get; set; }
        public GroupLayer       Parent       { get; set; }

        public bool IsStarted
        {
            get { return isStarted; }
            set
            {
                isStarted = value;
                OnPropertyChanged("IsStarted");
            }
        }

        public string State
        {
            get { return state; }
            set
            {
                state = value;
                OnPropertyChanged("State");
            }
        }

        public bool CanStart
        {
            get { return canStart; }
            set
            {
                canStart = value;
                OnPropertyChanged("CanStart");
            }
        }

        public bool CanStop
        {
            get { return canStop; }
            set
            {
                canStop = value;
                OnPropertyChanged("CanStop");
            }
        }

        public void Start(bool share = false)
        {
            IsLoading = true;
            CanStop = true;
        }

        public void Stop()
        {
            Visible = false;
        }

        public event EventHandler Stopped;

        public event EventHandler Started;

        protected void TriggerStarted()
        {
            IsStarted = true;
            if (Started != null) Started(this, null);
        }

        protected void TriggerStopped()
        {
            IsStarted = false;
            if (Stopped != null) Stopped(this, null);
        }

        private bool isLoading;

        public bool IsLoading
        {
            get { return isLoading; }
            set
            {
                if (isLoading == value) return;
                isLoading = value; OnPropertyChanged("IsLoading");
                if (value && StartedLoading != null) StartedLoading(this, null);
                if (!value && StoppedLoading != null) StoppedLoading(this, null);
            }
        }

        private Dictionary<string, IModel> models = new Dictionary<string, IModel>();

        public Dictionary<string, IModel> Models
        {
            get { return models; }
            set { models = value; }
        }

        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        private bool isTabActive;

        public bool IsTabActive
        {
            get { return isTabActive; }
            set { isTabActive = value; OnPropertyChanged("IsTabActive"); }
        }

        public PoiService Service
        {
            get { return service; }
            set { service = value; }
        }

        public void Init()
        {
            if (Service == null) return;
            CreateModels();
            //AppState.TimelineManager.TimeChanged += TimelineManagerFocusTimeChanged;
            //AppState.TimelineManager.TimeContentChanged += TimelineManagerFocusTimeChanged;
            if (AppState.TimelineManager != null)
            {
                AppState.TimelineManager.FocusTimeThrottled += TimelineManagerFocusTimeChanged;
            }


            EventList = new EventList { Name = Service.Name };
            AppState.EventLists.AddEventList(EventList);

            EventList.Clicked += EventList_Clicked;
        }

        /// <summary>
        /// Reorder layer list based on service settings
        /// </summary>
        public void CheckLayerOrder()
        {
            if (string.IsNullOrEmpty(Service.Settings.LayerOrder)) return;
            // find defined layer order
            var ll = Service.Settings.LayerOrder.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            // check if there is a default position 
            if (!ll.Contains("*")) ll.Add("*");
            var ol = new Dictionary<string, int>();

            // determine position of each layer
            foreach (var l in ChildLayers)
            {
                if (ll.Contains(l.ID))
                {
                    ol[l.ID] = ll.IndexOf(l.ID);
                }
                else if ((l.ID == Service.Name) && ll.Contains("[default]"))
                {
                    ol[l.ID] = ll.IndexOf("[default]");
                }
                else
                {
                    ol[l.ID] = ll.IndexOf("*");
                }
            }

            var i = 0;
            foreach (var o in ol.OrderBy(k => k.Value))
            {
                var l = ChildLayers.FirstOrDefault(k => k.ID == o.Key);
                var ind = ChildLayers.IndexOf(l);
                if (ind != i)
                {
                    ChildLayers.Move(ind, i);
                }
                i += 1;
            }
        }

        void EventList_Clicked(object sender, EventList.EventClickedArgs e)
        {
            var id = e.e.Id;
            var p = Service.PoIs.FirstOrDefault(k => k.Id == id);
            if (p != null)
            {

                Service.TriggerPing(p);
            }
        }

        void CreateModels()
        {
            //Models = new Dictionary<string, IModel>();
            //foreach (var m in plugin.Models)
            //{
            //}
        }

        public void UpdatePosition(BaseContent p)
        {

        }

        

        public void AddPoi(PoI p)
        {
            if (p.PoiType != null) p.PoiType.TriggerItemsLeft();
            var mm = p.FindModels();
            if (mm != null)
            {
                foreach (var m in mm)
                {
                    if (!Models.ContainsKey(m.Id) && plugin.Models.ContainsKey(m.Type))
                    {
                        var pm = plugin.Models[m.Type];
                        var mi = Activator.CreateInstance(pm) as IModel;
                        Debug.Assert(mi != null, "Failed to create model instance");
                        if (mi == null) continue;
                        mi.Service = Service;
                        mi.Id = m.Id;
                        mi.Model = m;
                        mi.DataServer = plugin.Dsb;
                        mi.Layer = this;
                        Models[m.Id] = mi;
                        mi.Start();
                    }
                    if (!Models.ContainsKey(m.Id)) continue;
                    var instance = Models[m.Id].GetPoiInstance(p);
                    instance.Start();
                    p.RaiseOnModelLoaded(instance);
                }
            }

            if (p.Labels.ContainsKey("mobile"))
            {
                p.PositionChanged += p_PositionChanged;
            }
            if (p.OpenOnAdd)
            {
                p.OpenOnAdd = false;
                p.Select();

            }

            if (string.IsNullOrEmpty(p.NEffectiveStyle.ShowOnTimeline)) return;
            if (EventList.Any(k => k.Id == p.Id)) return;
            var eb = new EventBase
            {
                Date = p.Date,
                Id = p.Id,
                Category = p.NEffectiveStyle.ShowOnTimeline,
                AlwaysShow = true,
                Image = p.NEffectiveStyle.Picture,
                Parent = EventList,
                Name = p.TimelineString
            };
            if (p.Position != null)
            {
                eb.Latitude = p.Position.Latitude;
                eb.Longitude = p.Position.Longitude;
            }

            EventList.Add(eb);

           
        }

        private DateTime lastPositionUpdate = DateTime.Now;

        void p_PositionChanged(object sender, PositionEventArgs e)
        {
            if (lastPositionUpdate.AddSeconds(3) >= DateTime.Now) return;
            var p = (PoI)sender;
            p.AddSensorData("[lat]", DateTime.Now, e.Position.Latitude);
            p.AddSensorData("[lon]", DateTime.Now, e.Position.Longitude);
            p.AddSensorData("Accuracy", DateTime.Now, e.Position.Accuracy);
            p.AddSensorData("Altitude", DateTime.Now, e.Position.Altitude);
            lastPositionUpdate = DateTime.Now;
        }

        public void RemovePoi(PoI p)
        {
            p.PositionChanged -= p_PositionChanged;
            if (p.PoiType != null) p.PoiType.TriggerItemsLeft();
            var mm = p.FindModels();
            if (mm != null)
            {
                foreach (var m in mm.Where(m => Models.ContainsKey(m.Id)))
                {
                    Models[m.Id].RemovePoiInstance(p);
                }
            }

            if (string.IsNullOrEmpty(p.NEffectiveStyle.ShowOnTimeline)) return;
            var e = EventList.FirstOrDefault(k => k.Id == p.Id);
            if (e != null) EventList.Remove(e);
            //AppState.TimelineManager.ForceTimeContentChanged();
        }

        public void RefreshAllVisiblePois(bool force = true)
        {
            PoIStyle.Count = 0;
            PoIStyle.Mergetime = 0;
            var changed = force;
            //lock (refreshLock)
            {
                try
                {
                    var vp = Service.ContentInExtent.ToList();
                    for (int i = 0; i < vp.Count; i++)
                    {
                        var p = (PoI)vp[i];
                        if (p.UpdateAnalysisStyle()) changed = true;
                    }
                    //Parallel.ForEach(vp, (baseContent, loopState) =>
                    //{
                    //    var p = (PoI)baseContent;
                    //    if (p.UpdateAnalysisStyle()) changed = true;
                    //});


                    if (changed)
                    {
                        Execute.OnUIThread(() =>
                        {
                            foreach (var baseContent in vp)
                            {
                                var p = (PoI)baseContent;
                                p.TriggerUpdated();
                            }
                        });
                    }
                }
                catch (Exception e)
                {
                    Logger.Log("dsBaseLayer", "RefreshAllVisiblePois error", e.Message, Logger.Level.Error, true);
                }
            }

            //Execute.OnUIThread(() =>
            //{
            //    AppState.TriggerNotification(sw.ElapsedTicks.MilSecStringFromTicks()
            //        + " - " + BaseContent.BaseStyle.MilSecStringFromTicks()
            //        + ", " + BaseContent.Merge1.MilSecStringFromTicks()
            //        + "," + BaseContent.Merge2.MilSecStringFromTicks()
            //        + "," + BaseContent.Merge3.MilSecStringFromTicks()
            //        + "," + BaseContent.Merge4.MilSecStringFromTicks()
            //        + "," + PoIStyle.Mergetime.MilSecStringFromTicks() 
            //        + ",c:" + PoIStyle.Count
            //        + ",t:" + Service.ContentInExtent.Count);
            //});

        }

        private DateTime lastUpdateTime;
        public void TimelineManagerFocusTimeChanged(object sender, EventArgs e)
        {
            if (!service.PoIs.Any() || !service.HasSensorData || !service.IsInitialized) return;

            if ((DateTime.Now - lastUpdateTime).TotalMilliseconds < 500) return;
            lastUpdateTime = DateTime.Now;

            var timelineManager = sender as TimelineManager;
            if (timelineManager == null) return;

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    var poschanged = new ConcurrentBag<BaseContent>();
                    var sensorchanged = new ConcurrentBag<BaseContent>();
                    var exceptions = new ConcurrentQueue<Exception>();
                    Parallel.ForEach(service.PoIs, (p, loopState) =>
                    {
                        try
                        {
                            if (!service.PoIs.Any() || !service.HasSensorData || !service.IsInitialized)
                                loopState.Break();
                            if (p.Sensors == null || !p.Sensors.Any()) return;
                            foreach (var s in p.Sensors.Where(s => s.Value != null && s.Value.SetFocusDate(AppState.TimelineManager.FocusTime)).Where(s => Service.ContentInExtent.Contains(p)))
                            {
                                sensorchanged.Add(p);
                            }
                            if (!p.Sensors.ContainsKey("[Lon]") || !p.Sensors.ContainsKey("[Lat]")) return;
                            var lo = p.Sensors["[Lon]"].FocusValue;
                            var la = p.Sensors["[Lat]"].FocusValue;
                            //p.Position = new Position(lo, la);
                            if (lo == p.Position.Longitude && la == p.Position.Latitude) return;
                            p.Position.Latitude = la;
                            p.Position.Longitude = lo;
                            poschanged.Add(p);
                        }

                        // Store the exception and continue with the loop.                     
                        catch (Exception err)
                        {
                            exceptions.Enqueue(err);
                        }
                    });
                    if (!service.PoIs.Any() || !service.HasSensorData || !service.IsInitialized)
                        return;

                    Execute.OnUIThread(() =>
                    {
                        foreach (var p in poschanged)
                        {
                            p.TriggerPositionChanged();
                            p.Updated = timelineManager.FocusTime;
                        }
                        foreach (var p in sensorchanged.Where(p => p != null)) p.TriggerUpdated();
                    });
                }
                catch (Exception es)
                {
                    Logger.Log("Poi Layer", "Error updating poi Layer", es.Message, Logger.Level.Error);
                }
                finally
                {
                    RefreshAllVisiblePois();
                }
            });
        }

        public void OpenTab(int index = 1)
        {
            if (Service.Settings == null) return;
            var viewModel = new ContentListViewModel { Dsb = plugin.Dsb, Service = service, SelectedTab = index };
            var image = string.IsNullOrEmpty(Service.Settings.Icon)
                ? new BitmapImage(new Uri("pack://application:,,,/csCommon;component/Resources/Icons/content.png"))
                : FileStore.LoadPhoto(Service.store.GetBytes(Service.MediaFolder, Service.Settings.Icon));
            tabItem = new StartPanelTabItem
            {
                ModelInstance = viewModel,
                Position = StartPanelPosition.left,
                HeaderStyle = TabHeaderStyle.Image,
                Image = image,
                Name = service.Name
            };
            AppState.AddStartPanelTabItem(tabItem);
            IsTabActive = true;
        }

        public void CloseTab()
        {
            if (tabItem == null || !AppState.StartPanelTabItems.Contains(tabItem)) return;
            AppState.RemoveStartPanelTabItem(tabItem);
            IsTabActive = false;
        }

        public void CloneLayer(FrameworkElement sb)
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
                if (AppState.DataServer.Services.Any(k=>k.Name == ea.Result))
                {
                    AppState.TriggerNotification("Please select unique name");
                    return;
                }
                var d = DateTime.Now;
                var SaveName = ea.Result;
                csCommon.Logging.LogCs.LogMessage(String.Format("CloneLayer: Create PoiService '{0}'", "empty"));
                var ss = new PoiService()
                {
                    IsLocal = true,
                    Name = SaveName,
                    Id = Guid.NewGuid(),
                    IsFileBased = true,
                    StaticService = this.Service.StaticService,
                    IsVisible = false,
                    Folder = this.Service.Folder,
                    RelativeFolder = this.Service.RelativeFolder
                };


                ss.Init(Mode.client, AppStateSettings.Instance.DataServer);
                ss.RelativeFolder = this.Service.RelativeFolder;
                ss.InitPoiService();
                ss.SettingsList = new ContentList
                {
                    Service = ss,
                    ContentType = typeof(ServiceSettings),
                    Id = "settings",
                    IsRessetable = false
                };
                ss.SettingsList.Add(new ServiceSettings());
                ss.AllContent.Add(ss.SettingsList);
                ss.Settings.OpenTab = this.Service.Settings.OpenTab;
                ss.Settings.TabBarVisible = Service.Settings.TabBarVisible;
                ss.Settings.Icon = Service.Settings.Icon;
                ss.AutoStart = Service.AutoStart;



                foreach (var pt in this.Service.PoITypes) ss.PoITypes.Add(pt);
                foreach (var p in this.Service.PoIs) ss.PoIs.Add(p);
                ss.SaveXml();
                AppStateSettings.Instance.DataServer.AddService(ss, Mode.client);
            };
            AppStateSettings.Instance.Popups.Add(input);
            
        }

        public void ExportLayerData()
        {
            var result = SaveSupportedFileDialog.BrowseAndSaveFile(Service);
            if (result == null) return;
            if (result.Successful)
            {
                MessageBox.Show(Application.Current.MainWindow, "Export successful:\n" + result.Result, "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(Application.Current.MainWindow, "Export error:\n" + result.Exception, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);     // TODO Showing the exception is a bit ugly.           
            }
        }

        public List<System.Windows.Controls.MenuItem> GetMenuItems()
        {
            var r = service.GetMenuItems();

            if (!service.IsSubscribed) return r;
            if (IsTabActive)
            {
                var closetab = MenuHelpers.CreateMenuItem("Close Tab", MenuHelpers.FolderIcon);
                closetab.Click += (e, f) => CloseTab();
                r.Add(closetab);
            }
            else
            {
                var searchlayer = MenuHelpers.CreateMenuItem("Search Layer", MenuHelpers.SearchIcon);
                searchlayer.Click += (e, f) => OpenTab(1);
                r.Add(searchlayer);

                var analytics = MenuHelpers.CreateMenuItem("Analysis", MenuHelpers.FilterIcon);
                analytics.Click += (e, f) => OpenTab(2);
                r.Add(analytics);

                var export = MenuHelpers.CreateMenuItem("Export", MenuHelpers.FolderIcon);
                export.Click += (e, f) => ExportLayerData();
                r.Add(export);

                if (this.Service.IsFileBased && Service.IsLocal)
                {
                    var clone = MenuHelpers.CreateMenuItem("Clone", MenuHelpers.LayerIcon);
                    clone.Click += (e, f) => CloneLayer(clone);
                    r.Add(clone);
                }
            }
            //var mi = MenuPopupViewModel.CreateMenuItem("Test Item");
            //mi.Click += (e, f) =>
            //{

            //};
            //r.Add(mi);
            return r;
        }
    }

    public static class TimeSpanExtensions
    {
        public static string MilSecStringFromTicks(this long ticks)
        {
            return (ticks / (double)TimeSpan.TicksPerMillisecond).ToString("####.#", CultureInfo.InvariantCulture);
        }
    }
}