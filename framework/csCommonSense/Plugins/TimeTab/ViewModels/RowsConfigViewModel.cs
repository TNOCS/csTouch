using Caliburn.Micro;
using csShared;
using csShared.Geo;
using csShared.Timeline;

namespace csTimeTabPlugin
{
    public class RowsConfigViewModel : Screen
    {
        private TimeTabViewModel timeTab;
        private RowsConfigView view;
        public TimeTabPlugin Plugin { get; set; }

        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public TimeTabViewModel TimeTab
        {
            get { return timeTab; }
            set
            {
                timeTab = value;
                NotifyOfPropertyChange(() => TimeTab);
            }
        }

        public TimelineManager TimelineManager
        {
            get { return AppState.TimelineManager; }
        }


        protected override void OnViewLoaded(object v)
        {
            base.OnViewLoaded(v);
            view = (RowsConfigView) v;
            //foreach (var i in TimelineManager.Rows)
            //{
            //    //view.fluidWrapPanel.Children.Add(new RowView() { DataContext = i});
            //}
            //vi.Items.ItemsSource = TimeItems;
        }

        public void Update()
        {
            TimeTab.ResetTimeline();
            AppState.TimelineManager.ForceTimeContentChanged();
        }
    }
}