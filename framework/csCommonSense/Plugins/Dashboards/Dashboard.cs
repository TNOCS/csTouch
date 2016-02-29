using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Media;
using Caliburn.Micro;
using DataServer;
using Newtonsoft.Json;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Drawing.ColorConverter;

namespace csCommon.Plugins.DashboardPlugin
{
    public class Dashboard : PropertyChangedBase
    {
        public event EventHandler Activated;
        public event EventHandler DeActivated;

        private bool isDefault;

        public bool IsDefault
        {
            get { return isDefault; }
            set { isDefault = value; NotifyOfPropertyChange(()=>IsDefault); }
        }

        private bool isEmpty;

        public bool IsEmpty
        {
            get { return isEmpty; }
            set { isEmpty = value; NotifyOfPropertyChange(()=>IsEmpty); }
        }
        

        private bool canEdit;

        public bool CanEdit
        {
            get { return canEdit; }
            set { canEdit = value; NotifyOfPropertyChange(()=>CanEdit); }
        }
        

        private bool isActive;

        public bool IsActive
        {
            get { return isActive; }
            set
            {
                if (isActive == value) return;
                
                isActive = value;
                if (isActive && Activated!=null) Activated(this, null);
                if (!isActive && DeActivated != null) DeActivated(this, null);
                NotifyOfPropertyChange("IsActive"); }
        }

        private string backgroundColor = "Map";

        public string BackgroundColor
        {
            get { return backgroundColor; }
            set
            {
                backgroundColor = value; NotifyOfPropertyChange(()=>BackgroundColor);
                if (value == "Map")
                {
                    BackgroundBrush = null;
                }
                else
                {
                    var o = ColorReflector.ToColorFromHex(value);
                    BackgroundBrush = new SolidColorBrush(o);
                }
                
            }
        }
        

        private BindableCollection<DashboardItem> dashboardItems = new BindableCollection<DashboardItem>();


        public BindableCollection<DashboardItem> DashboardItems
        {
            get { return dashboardItems; }
            set { dashboardItems = value; NotifyOfPropertyChange(() => DashboardItems); }
        }

        private bool hideMap;

        public bool HideMap
        {
            get { return hideMap; }
            set { hideMap = value; NotifyOfPropertyChange(()=>HideMap); }
        }

        private bool hideTimeline;

        public bool HideTimeline
        {
            get { return hideTimeline; }
            set { hideTimeline = value; NotifyOfPropertyChange(()=>HideTimeline); }
        }

        private double opacity = 1.0;

        public double Opacity
        {
            get { return opacity; }
            set { opacity = value; NotifyOfPropertyChange(()=>Opacity); }
        }



        private System.Windows.Media.Brush backgroundBrush = System.Windows.Media.Brushes.Transparent;

        [JsonIgnore]
        public System.Windows.Media.Brush BackgroundBrush
        {
            get { return backgroundBrush; }
            set { backgroundBrush = value; NotifyOfPropertyChange(()=>BackgroundBrush); }
        }
        

        private int gridWidth;

        public int GridWidth
        {
            get { return gridWidth; }
            set { gridWidth = value; NotifyOfPropertyChange(()=>GridWidth); }
        }

        private int gridHeight;

        public int GridHeight
        {
            get { return gridHeight; }
            set { gridHeight = value; NotifyOfPropertyChange(()=>GridHeight); }
        }

        private string title;

        public string Title
        {
            get { return title; }
            set { title = value; NotifyOfPropertyChange(()=>Title); }
        }



        public event EventHandler DashboardChanged; 


        internal void TriggerChanged()
        {
            if (DashboardChanged != null) DashboardChanged(this, null);
        }
    }
}
