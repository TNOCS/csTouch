using System.Windows;
using Caliburn.Micro;
using csShared;
using csTimeTabPlugin;

namespace csCommon.Plugins.TimeTab.ViewModels
{
    public class EventMessagesViewModel : Screen
    {
        private TimeTabViewModel timeTab;
        public TimeTabPlugin Plugin { get; set; }

        private static AppStateSettings AppState
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

        public BindableCollection<TimeItemViewModel> Items { get { return TimeTab.TimeItems; } }

        /// <summary>
        /// Jump to the actual location (not to the time)
        /// </summary>
        /// <param name="item"></param>
        public void JumpToLocation(TimeItemViewModel item)
        {
            if (item.Item == null) return;
            AppState.ViewDef.PanAndPoint(new Point(item.Item.Longitude, item.Item.Latitude));
        }
    }
}