using System.Windows.Media;
using Caliburn.Micro;
using csEvents.Sensors;
using DataServer;
using Newtonsoft.Json;

namespace csCommon.Plugins.DashboardPlugin
{

    public interface IDashboardItemViewModel
    {
        DashboardItem Item { get; set; }
         string Config { get; set; }
         IScreen ConfigScreen { get; set; }
    }

    public enum ViewTypes
    {
        Text,
        BarChart,
        LineChart,
        PieChart
    }

    public class ServiceDashboardItem : DashboardItem
    {
        private PoiService service;

        [JsonIgnore]
        public PoiService Service
        {
            get { return service; }
            set { service = value; NotifyOfPropertyChange(()=>Service); }
        }

        private string serviceId;

        public string ServiceId
        {
            get { return serviceId; }
            set { serviceId = value; NotifyOfPropertyChange(()=>ServiceId); }
        }
        
        
    }

    public class DashboardItem : PropertyChangedBase
    {

        private string title;

        public string Title
        {
            get { return title; }
            set { title = value; NotifyOfPropertyChange(()=>Title); }
        }

        private string dashboardtype;

        public string Type
        {
            set
            {
                dashboardtype = value;
            }
            get
            {
                if (string.IsNullOrEmpty(dashboardtype))
                {
                    var t = ViewModel.GetType();
                    return t.ToString() + "," + t.Assembly;
                }
                return dashboardtype;
            }
        }

        
        

        private IDashboardItemViewModel viewModel;

        [JsonIgnore]
        public IDashboardItemViewModel ViewModel
        {
            get { return viewModel; }
            set { viewModel = value; NotifyOfPropertyChange(()=>ViewModel); }
        }

        private int gridX;

        public int GridX
        {
            get { return gridX; }
            set { gridX = value; NotifyOfPropertyChange(()=>GridX); }
        }

        private int gridY;

        public int GridY
        {
            get { return gridY; }
            set { gridY = value; NotifyOfPropertyChange(()=>GridY); }
        }

        private int gridHeight;

        public int GridHeight
        {
            get { return gridHeight; }
            set { gridHeight = value; NotifyOfPropertyChange(()=>GridHeight); }
        }

        private int gridWidth;

        public int GridWidth
        {
            get { return gridWidth; }
            set { gridWidth = value; NotifyOfPropertyChange(()=>GridWidth); }
        }

        private Dashboard dashboard;

        [JsonIgnore]
        public Dashboard Dashboard
        {
            get { return dashboard; }
            set { dashboard = value; NotifyOfPropertyChange(()=>Dashboard);}
        }

       
        

        private DataSet data;

        [JsonIgnore]
        public DataSet Data
        {
            get { return data; }
            set { data = value; NotifyOfPropertyChange(()=>Data); }
        }

        private string dataSetId;

        public string DataSetId
        {
            get { return dataSetId; }
            set { dataSetId = value; NotifyOfPropertyChange(()=>DataSetId); }
        }
        

        private string config;

        public string Config
        {
            get
            {
                
                return config;
            }
            set
            {
                
                config = value;
                NotifyOfPropertyChange(()=>Config);
            }
        }

        private string view;

        public string View
        {
            get { return view; }
            set { view = value; }
        }

        private string backgroundString = "White";

        public string BackgroundString
        {
            get { return backgroundString; }
            set { backgroundString = value;
               var o = ColorReflector.ToColorFromHex(value);
                BackgroundBrush = new SolidColorBrush(o);
                NotifyOfPropertyChange(()=>BackgroundString); }
        }

        private Brush backgroundBrush = Brushes.White;

        [JsonIgnore]
        public Brush BackgroundBrush
        {
            get { return backgroundBrush; }
            set { backgroundBrush = value; 
                NotifyOfPropertyChange(()=>BackgroundBrush); }
        }
        
        
        

    }
}
