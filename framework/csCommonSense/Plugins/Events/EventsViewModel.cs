using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro;
using System;
using System.Linq;
using Microsoft.Surface.Presentation.Controls;
using csEvents;
using csCommon.csMapCustomControls.CircularMenu;
using csShared;
using csShared.Controls.Popups.MenuPopup;
using csShared.FloatingElements;

namespace csCommon.Plugins.Events
{
    public enum EventsSortingOptions
    {
        Time,
        Source,
        Status
    }

    [Export(typeof(IEvents))]
    public class EventsViewModel : Screen
    {
        readonly AppStateSettings appState = AppStateSettings.GetInstance();
        
        public event EventHandler Addevent;
        public void TriggerAdd(object sender)
        {
            Addevent?.Invoke(sender, null);
        }

        private IEvent selectedEvent;
        public IEvent SelectedEvent
        {
            get { return selectedEvent; }
            set { selectedEvent = value; NotifyOfPropertyChange(()=>SelectedEvent);
                if (value == null) return;
                SelectedEvent.TriggerClicked(value, "selected");
            }
        }
        //TODO fixed FilteredList by Jeroen
        //public EventList Events { get { return appState.EventLists.First(); } }


        private bool showSortOrder=true;

        public bool ShowSortOrder
        {
            get { return showSortOrder; }
            set { showSortOrder= value; NotifyOfPropertyChange(()=>ShowSortOrder); }
        }
        

        private BindableCollection<IEvent> alarms = new BindableCollection<IEvent>();
        public BindableCollection<IEvent> Alarms
	    {
		    get { return alarms;}
		    set { alarms = value; NotifyOfPropertyChange(()=>Alarms);}
	    }

        private bool mapFilter = true;

        public bool MapFilter
        {
            get { return mapFilter; }
            set { mapFilter = value; NotifyOfPropertyChange(()=>MapFilter); }
        }

        private bool timeFilter = true;

        public bool TimeFilter
        {
            get { return timeFilter; }
            set { timeFilter = value; NotifyOfPropertyChange(()=>TimeFilter); }
        }
        
        public void Add()
        {
            
        }

        private readonly FloatingElement f;

        public EventsViewModel()
        {
            f = FloatingHelpers.CreateFloatingElement("Events", new Point(300, 300), new Size(400, 400), this);
            f.MinSize = new Size(400, 400);
        }

        public EventsView EventView;

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            EventView = (EventsView) view;
            UpdateList();

            appState.EventLists.CollectionChanged     += Alarms_CollectionChanged;
            appState.EventLists.NewEvent              += EventLists_NewEvent;
            appState.ViewDef.MapControl.ExtentChanged += MapControl_ExtentChanged;
        }

        //private void TimelineManager_TimeContentChanged(object sender, TimeEventArgs e)
        //{
        //    UpdateList();
        //}

        //void TimelineManager_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        //{
        //    if (e.PropertyName == "Start" || e.PropertyName == "End")
        //        UpdateList();
        //}

        void MapControl_ExtentChanged(object sender, ESRI.ArcGIS.Client.ExtentEventArgs e)
        {
            UpdateList();
        }

        public void SetTemplate(DataTemplateSelector ts)
        {
            if (EventView == null) return;
            EventView.Alarms.ItemTemplate = null;
            EventView.Alarms.ItemTemplateSelector = ts;
        }

        void EventLists_NewEvent(object sender, NewEventArgs e)
        {
            UpdateList();
        }

        void Alarms_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            foreach (var el in appState.EventLists)
                el.Clicked += el_Clicked;
            UpdateList();
        }

        void el_Clicked(object sender, EventList.EventClickedArgs e)
        {
            UpdateList();
        }

        public void OpenSortingMenu(EventsViewModel st, SurfaceButton b)
        {
            var menu = new MenuPopupViewModel {
                RelativeElement = b,
                RelativePosition = new Point(0, 0),
                TimeOut = new TimeSpan(0, 0, 0, 5),
                VerticalAlignment = VerticalAlignment.Bottom,
                AutoClose = true
            };
            //menu.Point = _view.CreateLayer.TranslatePoint(new Point(0,0),Application.Current.MainWindow);
            //menu.DisplayProperty = "ServiceName";

            foreach (var a in Enum.GetValues(typeof(EventsSortingOptions)))
            {
                var mi = menu.AddMenuItem(a.ToString());
                var eventsSortingOption = (EventsSortingOptions)a;
                mi.Click += (e, s) => {
                    CurrentSortingOrder = eventsSortingOption;
                    UpdateList(CurrentSortingOrder);
                    menu.Close();
                };
            }
            appState.Popups.Add(menu);
        }

        private EventsSortingOptions currentSortingOrder;

        public EventsSortingOptions CurrentSortingOrder
        {
            get { return currentSortingOrder; }
            set { currentSortingOrder = value; NotifyOfPropertyChange(()=>CurrentSortingOrder); }
        }

        public void UpdateList(bool force = false)
        {
            UpdateList(CurrentSortingOrder, force);
        }

        private DateTime refreshTime = DateTime.Now;
        
        void UpdateList(EventsSortingOptions order, bool force = false)
        {
            if ((DateTime.Now - refreshTime).TotalMilliseconds < 50 && !force) 
                return;
            refreshTime = DateTime.Now;

            CurrentSortingOrder = order;

            var list = appState.EventLists.GetAllFilteredEvents();
            
            
            //foreach (var a in AppState.Alarms)
            //    {
            //        list.Add(a);
            //        var e = a as EventBase;
            //    if (e!=null)
            //        e.PropertyChanged += e_PropertyChanged;
            //    }
            Alarms.Clear();
            switch (order) {
                case EventsSortingOptions.Time:
                    Alarms.AddRange(list.OrderByDescending(k => k.Date));
                    break;
                case EventsSortingOptions.Source:
                    Alarms.AddRange(list.OrderByDescending(k => k.Name));
                    break;
                case EventsSortingOptions.Status:
                    Alarms.AddRange(list.OrderByDescending(k => k.State));
                    break;
                default:
                    Alarms.AddRange(list.OrderByDescending(k => k.Date));
                    break;
            }
        }

        //void e_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        //{
        //    UpdateList(CurrentSortingOrder);
        //}

        public void menu_Selected(object sender, MenuItemEventArgs e)
        {
            {
                Execute.OnUIThread(()=>
                {
                    var p = e.Menu.PointToScreen(new Point(0, 0));
                    f.StartPosition = new Point(p.X+300,p.Y+300);
                    if (!appState.FloatingItems.Contains(f))
                        appState.FloatingItems.AddFloatingElement(f);
                    else
                        appState.FloatingItems.RemoveFloatingElement(f);
                });
            }
        }

    }
}
