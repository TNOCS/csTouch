using Caliburn.Micro;

namespace csCommon.Plugins.DashboardPlugin
{
    public class ChartViewStyle : PropertyChangedBase
    {
        private ViewTypes type;

        public ViewTypes Type
        {
            get { return type; }
            set { type = value; NotifyOfPropertyChange(()=>Type); }
        }

        private string title;

        public string Title
        {
            get { return title; }
            set { title = value; NotifyOfPropertyChange(()=>Title); }
        }
        

        public string[] Sensors { get; set; }
    }
}