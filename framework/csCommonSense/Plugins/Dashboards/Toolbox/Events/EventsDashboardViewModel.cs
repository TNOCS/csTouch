using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Threading;
using Caliburn.Micro;
using csCommon.Plugins.DashboardPlugin;
using csEvents;
using csShared;
using DataServer;

namespace csCommon.Plugins.Dashboards.Toolbox.Events
{
    [Export(typeof (Screen))]
    public class EventsDashboardViewModel : Screen, IDashboardItemViewModel
    {
        public override string DisplayName { get; set; }

        public DataServerBase DataServer { get; set; }

        private IScreen configScreen;

        public IScreen ConfigScreen
        {
            get { return configScreen; }
            set
            {
                configScreen = value;
                NotifyOfPropertyChange(() => ConfigScreen);
            }
        }

        private IEvent lastEvent;

        public IEvent LastEvent
        {
            get { return lastEvent; }
            set { lastEvent = value; NotifyOfPropertyChange(()=>LastEvent); }
        }
        

        private DashboardItem item;

        public DashboardItem Item
        {
            get { return item; }
            set { item = value; NotifyOfPropertyChange(()=>Item); }
        }
        

        private static AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

      
        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            AppState.EventList.CollectionChanged += EventList_CollectionChanged;
            //var dt = new DispatcherTimer {Interval = new TimeSpan(0, 0, 5)};
            //dt.Tick += (e, f) => AppState.EventList.Add(new EventBase()
            //{
            //    Name           = "Name",
            //    Description    = "Description " + DateTime.Now,
            //    ShowOnTimeline = true,
            //    Category       = "Category",
            //    Date           = DateTime.Now
            //});
            //dt.Start();
            //FindLastEvent();
        }

        void EventList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            FindLastEvent();
        }

        void EventLists_FilteredListUpdated(object sender, System.EventArgs e)
        {
            FindLastEvent();
        }

        private void FindLastEvent()
        {
            LastEvent = (from el in AppState.EventLists.Where(k => k.Any())
                from a in el.OrderByDescending(k => k.Date)
                select a).OrderByDescending(k => k.Date).FirstOrDefault();
        }

        private string config;

        public string Config
        {
            get
            {
                return config;
                
            }
            set { config = value; NotifyOfPropertyChange(()=>Config); }
        }
    }

}
