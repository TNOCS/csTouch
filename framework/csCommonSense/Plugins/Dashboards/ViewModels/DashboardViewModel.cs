using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using csShared;


namespace csCommon.Plugins.DashboardPlugin
{
	

    [Export(typeof(IScreen))]
    public class DashboardViewModel : Screen
    {
        private DashboardView view;

        public AppStateSettings AppState { get { return AppStateSettings.Instance; }}

        private Dashboard dashboard;

        public Dashboard Dashboard
        {
            get { return dashboard; }
            set { dashboard = value; NotifyOfPropertyChange(()=>Dashboard); }
        }

        public System.Windows.Media.Brush BackgroundBrush  {
            get
            {
                return Dashboard.BackgroundBrush;
            }
            set { }
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            UpdateScreen();
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            this.view = (DashboardView)view;
            
            
            Dashboard.PropertyChanged += Dashboard_PropertyChanged;
            Dashboard.DashboardChanged += Dashboard_DashboardChanged;
            Dashboard.Activated += Dashboard_Activated;
            UpdateScreen();
        }

        void Dashboard_Activated(object sender, System.EventArgs e)
        {
            UpdateScreen();
        }

        void Dashboard_DashboardChanged(object sender, System.EventArgs e) 
        {
            //Task task = AppStateSettings.Instance.Dashboards.Save(); // REVIEW TODO fix changed await to task.Wait() as the event handler cannot be asynchronous.
            //task.Wait();
            AppStateSettings.Instance.Dashboards.Save(); 
        }

        public void UpdateScreen()
        {
            AppState.CirculairMenusVisible = !Dashboard.HideMap;
            AppState.TimelineManager.Visible = !Dashboard.HideTimeline; // TimelineVisible is deprecated.
            AppState.DockedFloatingElementsVisible = !Dashboard.HideMap;
            AppState.ViewDef.MapControl.Visibility = (Dashboard.HideMap) ? Visibility.Collapsed : Visibility.Visible;
        }

        void Dashboard_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateScreen();
        }

    }
}
