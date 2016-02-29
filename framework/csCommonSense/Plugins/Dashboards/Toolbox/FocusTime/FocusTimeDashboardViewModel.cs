using System.ComponentModel.Composition;
using Caliburn.Micro;
using csCommon.Plugins.DashboardPlugin;
//using csShared;

namespace csCommon.Plugins.Dashboards.Toolbox.FocusTime
{
    [Export(typeof (Screen))]
    public class FocusTimeDashboardViewModel : Screen, IDashboardItemViewModel
    {
        public override string DisplayName { get; set; }

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

        private DashboardItem item;

        public DashboardItem Item
        {
            get { return item; }
            set { item = value; NotifyOfPropertyChange(()=>Item); }
        }
        
        //private static AppStateSettings AppState
        //{
        //    get { return AppStateSettings.Instance; }
        //}

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
