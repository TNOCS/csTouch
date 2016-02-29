using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Caliburn.Micro;
using csCommon.Plugins.DashboardPlugin;
using csShared;
using csShared.Controls.Popups.MapCallOut;
using DataServer;

namespace csModels.Flow
{
  

    [Export(typeof(IScreen))]
    public class FlowViewModel : Screen, IEditableScreen, IDashboardItemViewModel
    {
        public override string DisplayName { get; set; }

        public DataServerBase DataServer { get; set; }

        public DashboardItem Item { get; set; }

        private IScreen configScreen;

        public FlowGraphConfigViewModel ChartConfig { get { return (FlowGraphConfigViewModel)ConfigScreen; } }


        public IScreen ConfigScreen
        {
            get { return configScreen; }
            set { configScreen = value; NotifyOfPropertyChange(() => ConfigScreen); }
        }

        private FlowCollection nextStops;

        public FlowCollection NextStops
        {
            get { return nextStops; }
            set { nextStops = value; NotifyOfPropertyChange(()=>NextStops); }
        }

        private ICollectionView fooView;

        public ICollectionView FooView
        {
            get
            {
                return this.fooView;
            }

            set
            {
                this.fooView = value;

                NotifyOfPropertyChange(() => FooView); 
            }
        }

        private static AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public void SelectFlow(FlowStop fs)
        {
            AppState.TriggerNotification("Flow selected");
        }

        private void FindPoi()
        {
            var service = AppState.DataServer.Services.FirstOrDefault(k => k.Name == "Demanes");
            if (service != null)
            {
                Poi = (PoI)(((PoiService)service).PoIs.FirstOrDefault(k => k.Name.ToLower().StartsWith("ect")));

            }
        }

        private string viewStyle;

        public string ViewStyle
        {
            get { return viewStyle; }
            set
            {
                viewStyle = value; NotifyOfPropertyChange(()=>ViewStyle);
            }
        }
        

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            if (Item == null) return;
            ConfigScreen = new FlowGraphConfigViewModel() {Item = Item};
            ChartConfig.ConfigChanged += ChartConfig_ConfigChanged;
            ViewStyle = Item.Config;
            if (Poi == null)
            {
                FindPoi();
                // find poi

                if (Poi == null)
                {
                    AppState.DataServer.Services.CollectionChanged += Services_CollectionChanged;
                }
                else
                {
                    Poi.InFlow().CollectionChanged += (e, f) =>
                    {
                        UpdateNextStops();
                    };    
                }
                
            }
            else
            {
                Poi.InFlow().CollectionChanged += (e, f) =>
                {
                    UpdateNextStops();
                };
            }
            
            UpdateNextStops();
            AppState.TimelineManager.FocusTimeThrottled += (e, f) =>
            {
                if (FooView!=null)
                    Execute.OnUIThread(UpdateNextStops);
            };

        }

        void ChartConfig_ConfigChanged(object sender, string e)
        {
            ViewStyle = Item.Config;
        }

        void Services_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            FindPoi();
            if (Poi != null)
            {
                AppState.DataServer.Services.CollectionChanged += Services_CollectionChanged;
                UpdateNextStops();
                Poi.InFlow().CollectionChanged += (te, f) =>
                {
                    UpdateNextStops();
                };
            }
        }

        public void UpdateNextStops()
        {
            if (Poi == null) return;

            NextStops = new FlowCollection();
            NextStops.AddRange(Poi.Flow());
            NextStops.AddRange(Poi.InFlow());

            foreach (var fs in NextStops)
            {
                var etd = fs.Etd;
                if (etd < fs.Eta) etd = AppState.TimelineManager.End;
                var w = 1000.0 / (AppState.TimelineManager.End - AppState.TimelineManager.Start).TotalSeconds;
                fs.FlowBarLeft = (fs.Eta - AppState.TimelineManager.Start).TotalSeconds*w;
                fs.FlowBarWidth = (etd - fs.Eta).TotalSeconds*w;


            }

            FooView = CollectionViewSource.GetDefaultView(NextStops);
            FooView.Filter = TimeFilter;
            FooView.SortDescriptions.Add(new SortDescription() { PropertyName = "Eta", Direction = ListSortDirection.Ascending });

            
            
        }

        private bool TimeFilter(object item)
        {
            var t = AppState.TimelineManager;
            FlowStop flowStop = item as FlowStop;
            return (flowStop.Eta >= t.Start && flowStop.Eta <= t.End);
        }

        private PoI poI;

        public PoI Poi
        {
            get { return poI; }
            set { poI = value; NotifyOfPropertyChange(() => Poi); }
        }

        //private string selectedColor = "Black";


        private bool canEdit;

        public bool CanEdit
        {
            get { return canEdit; }
            set
            {
                if (canEdit == value) return;
                canEdit = value;
                NotifyOfPropertyChange(() => CanEdit);
            }
        }

        public MapCallOutViewModel CallOut { get; set; }

        public string Config { get; set; }
    }

    public class EtaChartConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var t = new Thickness(20, 0, 0, 0);
            return t;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class FlowViewSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null) return null;

            var template = (container as FrameworkElement).FindResource("ListViewTemplate") as DataTemplate;
            return template;
        }
    }
}