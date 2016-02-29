using System.ComponentModel.Composition;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using csEvents;
using csShared;
using csShared.Controls.SlideTab;
using csShared.Geo;
using csShared.Interfaces;
using System;
using csCommon.csMapCustomControls.CircularMenu;
using System.Linq;
using ESRI.ArcGIS.Client.Geometry;
using csCommon.Plugins.Timeline;
using System.Collections.Generic;
using csShared.TabItems;

namespace csCommon.Plugins.Events
{
    [Export(typeof(IPlugin))]
    public class EventsPlugin : PropertyChangedBase, IPlugin
    {

        public event EventHandler Add;

        public void TriggerAdd(object sender) {
            var handler = Add;
            if (handler != null) handler(sender, null);
        }

        public bool CanStop { get { return true; } }

        private ISettingsScreen settings;

        public ISettingsScreen Settings
        {
            get { return settings; }
            set { settings = value; NotifyOfPropertyChange(() => Settings); }
        }

        private IPluginScreen screen;

        public IPluginScreen Screen
        {
            get { return screen; }
            set { screen = value; NotifyOfPropertyChange(() => Screen); }
        }

        private bool hideFromSettings;

        public bool HideFromSettings
        {
            get { return hideFromSettings; }
            set { hideFromSettings = value; NotifyOfPropertyChange(() => HideFromSettings); }
        }

        public int Priority
        {
            get { return 6; }
        }

        public string Icon
        {
            get { return @"icons\filewatcher.png"; }
        }

        #region IPlugin Members

        public string Name
        {
            get { return "Events"; }
        }

        private bool isRunning;
        public bool IsRunning
        {
            get { return isRunning; }
            set { isRunning = value; NotifyOfPropertyChange(() => IsRunning); }
        }

        public AppStateSettings AppState { get; set; }

        public FloatingElement Element { get; set; }

        public CircularMenuItem Menu { get; set; }

        private EventsViewModel viewmodel;

        public EventsViewModel ViewModel
        {
            get { return viewmodel; }
            set { viewmodel = value; NotifyOfPropertyChange(() => ViewModel); }
        }

        public void Init()
        {
            //var viewModel = IoC.GetAllInstances(typeof(IBookmark)).FirstOrDefault() as IBookmark;

            Menu = new CircularMenuItem
            {
                Id    = "Events",
                Title = "test",
                Icon  = "pack://application:,,,/csCommon;component/Resources/Icons/draw.jpg"//,
//                    Items = new List<CircularMenuItem>() {new CircularMenuItem(){Title = "test", Position = 1} }
            };

            ViewModel = (EventsViewModel)IoC.GetInstance(typeof(IEvents), "");
            if (ViewModel != null)
            {
                UpdateVisibility();
                Menu.Selected += ViewModel.menu_Selected;
                ViewModel.Addevent += ViewModel_Addevent;
            }
            if (envelope == null)
                envelope = AppState.ViewDef.MapControl.Extent;

            AppState.ViewDef.VisibleChanged += ViewDefVisibleChanged;
            AppState.ViewDef.MapControl.ExtentChanged += MapControl_ExtentChanged;
            AppState.TimelineManager.PropertyChanged += TimelineManager_PropertyChanged;
            //AppState.Alarms.CollectionChanged += Alarms_CollectionChanged;
            //AppState.EventLists.CollectionChanged+=Alarms_CollectionChanged;
            AppState.EventsLoaded += AppState_EventsLoaded;
            AppState.EventLists.NewEvent += Alarms_CollectionChanged;

            var spti = new StartPanelTabItem {
                ModelInstance = ViewModel,
                Position = StartPanelPosition.left,
                HeaderStyle = TabHeaderStyle.Image,
                Image = new BitmapImage(new Uri("pack://application:,,,/csCommon;component/Resources/Icons/logbook.png")),
                Name = "Events"
            };
            //spti.ImageUrl = @"icons\layers.png";

            AppState.AddStartPanelTabItem(spti);
        }

        void AppState_EventsLoaded(object sender, EventArgs e)
        {

            UpdateIcon(envelope);
            //UpdateTimeline(_envelope);
        }

        void MapControl_ExtentChanged(object sender, ESRI.ArcGIS.Client.ExtentEventArgs e)
        {
            UpdateIcon(e.NewExtent);
            //UpdateTimeline(e.NewExtent);
        }

        void Alarms_CollectionChanged(object sender, NewEventArgs e)
        {
            //
            //AppState.EventLists.NewEvent
            foreach (var el in AppState.EventLists)
            {
                //el.CollectionChanged += Alarms_CollectionChanged;
                el.Clicked += el_Clicked;
            }
            UpdateIcon(envelope);
            //UpdateTimeline(_envelope);
        }

        void el_Clicked(object sender, EventList.EventClickedArgs e)
        {
            UpdateIcon(envelope);
        }

        void ViewModel_Addevent(object sender, EventArgs e)
        {
            TriggerAdd(this);
        }

        private DateTime lastchecked;
        void TimelineManager_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Start" || lastchecked >= DateTime.Now.AddMilliseconds(-100)) return;
            UpdateIcon(envelope);
            lastchecked = DateTime.Now;
        }

        private Envelope envelope;

        void UpdateIcon()
        {
            var tlm     = AppState.TimelineManager;
            var danger  = AppState.EventLists.ToList().Any(k => k.ToList().Any(ev => ev.Name.Contains("danger:" ) && (ev.Date > tlm.CurrentTime.AddHours(-8) || (ev.Date > tlm.Start && ev.Date < tlm.End)) ));
            var anomaly = AppState.EventLists.ToList().Any(k => k.ToList().Any(ev => ev.Name.Contains("anomaly:") && (ev.Date > tlm.CurrentTime.AddHours(-8) || (ev.Date > tlm.Start && ev.Date < tlm.End)) ));
            SetMenuIcon(danger, anomaly);
        }

        void UpdateIcon(Envelope env)
        {
            if (env == null) return;
            envelope = env;

            var tlm     = AppState.TimelineManager;
            var danger  = AppState.EventLists.ToList().Any(k => k.ToList().Any(ev => !string.IsNullOrEmpty(ev.Name) && ev.Name.Contains("danger:" ) && (ev.Date > tlm.CurrentTime.AddHours(-8) || (ev.Date > tlm.Start && ev.Date < tlm.End)) && ev.InsideEnvelope(envelope)));
            var anomaly = AppState.EventLists.ToList().Any(k => k.ToList().Any(ev => !string.IsNullOrEmpty(ev.Name) && ev.Name.Contains("anomaly:") && (ev.Date > tlm.CurrentTime.AddHours(-8) || (ev.Date > tlm.Start && ev.Date < tlm.End)) && ev.InsideEnvelope(envelope)));
            SetMenuIcon(danger, anomaly);
        }

        private void SetMenuIcon(bool danger, bool anomaly) {
            Execute.OnUIThread(() => {
                if (danger)
                    Menu.Icon = "pack://application:,,,/csCommon;component/Resources/Icons/Delete.png";
                else if (anomaly)
                    Menu.Icon = "pack://application:,,,/csCommon;component/Resources/Icons/edit.png";
                else
                    Menu.Icon = "pack://application:,,,/csCommon;component/Resources/Icons/safe.png";
            });
        }

        private readonly List<IEvent> items = new List<IEvent>();
        private static readonly List<TimelineItem> TlItems = new List<TimelineItem>();

        void UpdateTimeline(Envelope env)
        {
            if (env == null)
                env = AppState.ViewDef.MapControl.Extent;
            envelope = env;
            var tlm = AppState.TimelineManager;

            var addlist = new List<IEvent>();
            var remlist = new List<IEvent>();
            foreach ( var el in AppState.EventLists.ToList())
            foreach (var e in el.ToList())
            {
                try
                {
                    if (e.InsideEnvelope(env)) {
                        if (items.All(k => k.Id != e.Id)) {
                            addlist.Add(e);
                        }
                    }
                    else {
                        if (items.All(k => k.Id != e.Id)) continue;
                        var r = items.FirstOrDefault(k => k.Id == e.Id);
                        remlist.Add(r);
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine(err.ToString());
                }
            }

            Execute.OnUIThread(() => {
                foreach (var r in remlist) {
                    var remitem = TlItems.FirstOrDefault(k => k.Id == r.Id);
                    if (remitem != null) {
                        if (TimelineView.TimelineViewInstance != null)
                            TimelineView.TimelineViewInstance.RemoveItemFromTimeline(remitem);
                        TlItems.Remove(remitem);
                    }
                    items.Remove(r);
                }
                foreach (var i in addlist) {
                    items.Add(i);
                    var tlItem = new TimelineItem(tlm) {
                        ItemRange    = i.TimeRange,
                        Text         = i.Name,
                        ItemDateTime = i.Date,
                        EventPoint   = new KmlPoint(i.Longitude, i.Latitude),
                        Id           = i.Id
                    };

                    TlItems.Add(tlItem);
                    if (TimelineView.TimelineViewInstance != null)
                        TimelineView.TimelineViewInstance.AddItemToTimeline(tlItem);
                    tlItem._tlManager_TimeChanged(null, null);
                }
            });
        }

        void ViewDefVisibleChanged(object sender, VisibleChangedEventArgs e)
        {
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (AppState.ViewDef.Visible && IsRunning)
            {
                if (!AppState.FloatingItems.Contains(Element)) AppState.FloatingItems.AddFloatingElement(Element);
            }
            else
            {
                if (AppState.FloatingItems.Contains(Element)) AppState.FloatingItems.RemoveFloatingElement(Element);
            }
        }

        public void Start()
        {
            IsRunning = true;
            UpdateVisibility();
        }

        public void Pause()
        {
            IsRunning = false;
            UpdateVisibility();
        }

        public void Stop()
        {
            IsRunning = false;
            UpdateVisibility();
        }
        
        #endregion
    }

    public interface IEvents
    {}
}
