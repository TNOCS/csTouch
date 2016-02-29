using System.ComponentModel.Composition;
using System.Linq;
using Caliburn.Micro;
using csShared;
using csShared.Interfaces;

namespace csCommon.Plugins.DashboardPlugin
{
    [Export(typeof(IPluginScreen))]
    public class DashboardsViewModel : Screen, IPluginScreen
    {
        private DashboardsView view;

        private DashboardsPlugin plugin;

        public DashboardsPlugin Plugin
        {
            get { return plugin; }
            set
            {
                plugin = value;
                
            }
        }

        public AppStateSettings AppState {get { return AppStateSettings.Instance; }}

        private BindableCollection<DashboardViewModel> dashboards = new BindableCollection<DashboardViewModel>();

        public BindableCollection<DashboardViewModel> Dashboards
        {
            get { return dashboards; }
            set { dashboards = value; }
        }




        private DashboardViewModel activeDashboard;

        public DashboardViewModel ActiveDashboard
        {
            get { return activeDashboard; }
            set { activeDashboard = value; NotifyOfPropertyChange(()=>ActiveDashboard); }
        }
        
        

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            this.view = (DashboardsView)view;

            AppState.Dashboards.CollectionChanged += DashboardViews_CollectionChanged;
            AppState.Dashboards.ActiveDashboardChanged += DashboardViews_ActiveDashboardChanged;

            foreach (var d in AppState.Dashboards)
            {
                AddDashboard(d);
            }

            
        }

        void DashboardViews_ActiveDashboardChanged(object sender, Dashboard e)
        {
            ActiveDashboard = Dashboards.FirstOrDefault(k => k.Dashboard == e);
        }

        void AddDashboard(Dashboard d)
        {
            var dv = new DashboardViewModel()
            {
                
                Dashboard = d

            };
            Dashboards.Add(dv);
        }

        void DashboardViews_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach ( Dashboard d in e.NewItems) AddDashboard(d);
            }
        }

        

        public string Name
        {
            get { return "Dashboards"; }
        }
    }
}
