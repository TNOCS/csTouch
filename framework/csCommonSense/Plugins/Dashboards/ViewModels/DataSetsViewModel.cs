#region

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using csCommon.Plugins.ServicesPlugin;
using csEvents.Sensors;
using csShared;
using DataServer;
using Task = System.Threading.Tasks.Task;

#endregion

namespace csCommon.Plugins.DashboardPlugin
{
    
    [Export(typeof (IServices))]
    public class DataSetsViewModel : Screen
    {
        private int selectedSensorSetIndex;

        private static AppStateSettings AppState {
            get { return AppStateSettings.GetInstance(); }
            set{}
        }

        private int selectedTab;

        public int SelectedTab
        {
            get { return selectedTab; }
            set { selectedTab = value; NotifyOfPropertyChange(()=>SelectedTab); }
        }
        

        public Brush AccentBrush {
            get { return AppState.AccentBrush; }
        }

        public string Category { get { return SelectedSensorSets == null ? string.Empty : SelectedSensorSets.Title; } }

        public DashboardsPlugin Plugin { get; set; }

        public SensorCollection SelectedSensorSets {
            get { return selectedSensorSetIndex < 0 || selectedSensorSetIndex >= SensorSets.Count ? null : SensorSets[selectedSensorSetIndex]; }
        }

        private DashboardViewModel activeDashboard;

	    public DashboardViewModel ActiveDashboard
	    {
		    get { return activeDashboard;}
		    set { activeDashboard = value; 
                NotifyOfPropertyChange(()=>ActiveDashboard);
            }
	    }

        public void BackgroundChanged()
        {
            
        }

        public async Task AddToolboxItem(DashboardItem item) // REVIEW TODO fix: async Task added
        {
            if (AppState.Dashboards.ActiveDashboard != null)
            {
                var di = new DashboardItem()
                {
                    Title = "Test 2x1",
                    GridX = 17,
                    GridY = 17,
                    GridHeight = 8,
                    GridWidth = 8,
                    Dashboard = AppState.Dashboards.ActiveDashboard,
                    Config = item.Config,                    
                };
                
                var t = Type.GetType(item.Type);
                if (t != null)
                {
                    di.ViewModel = (IDashboardItemViewModel)Activator.CreateInstance(t);
                    di.ViewModel.Item = di;

                }
                
                AppState.Dashboards.ActiveDashboard.DashboardItems.Add(di);
            }
            await AppState.Dashboards.Save("dashboards.config");
        }

        public void DeleteItem()
        {
            if (AppState.Dashboards.ActiveDashboardItem!=null && AppState.Dashboards.ActiveDashboardItem.Dashboard == AppState.Dashboards.ActiveDashboard)
                AppState.Dashboards.ActiveDashboard.DashboardItems.Remove(AppState.Dashboards.ActiveDashboardItem);
        }



        public async Task PinDataSet(DataSet sc) // REVIEW TODO fix: async added
        {
            if (AppState.Dashboards.ActiveDashboard != null)
            {
                var di = new DashboardItem()
                {
                    Title = "Test 2x1",
                    GridX = 17,
                    GridY = 17,
                    GridHeight = 8,
                    GridWidth = 8,
                    Data = sc,                    
                    Dashboard = AppState.Dashboards.ActiveDashboard,
                    Config = "Focus Value",
                    DataSetId = sc.DataSetId
                };
                di.ViewModel = new DashboardItemViewModel {Data = sc, Item = di};
                AppState.Dashboards.ActiveDashboard.DashboardItems.Add(di);
            }
            await AppState.Dashboards.Save("dashboards.config");
        }

        public void AddDashboard()
        {
            var dashboard = new Dashboard()
            {
                GridHeight = 36,
                GridWidth = 48,
                HideMap = false,
                BackgroundColor = "Map",
                Title = "Dashboard",
                DashboardItems = new BindableCollection<DashboardItem>()
            };
            AppState.Dashboards.Add(dashboard);
            AppState.Dashboards.ActiveDashboard = dashboard;
        }

        public void RemoveDashboard(Dashboard d)
        {
            if (AppState.Dashboards.ActiveDashboard == d)
            {
                AppState.Dashboards.ActiveDashboard = AppState.Dashboards.FirstOrDefault();
            }
            AppState.Dashboards.Remove(d);
        }

        public void SetDefaultDashboard(Dashboard d)
        {
            AppState.Dashboards.SetDefault(d);
        }

        public void Activate(Dashboard d)
        {
            AppState.Dashboards.ActiveDashboard = d;
            
        }

        public SensorCollectionSet SensorSets { get { return AppState.SensorSets; }
            set { }
        }


      
        public DashboardItem ActiveDashboardItem
        {
            get { return AppState.Dashboards.ActiveDashboardItem; }
            set
            {
                AppState.Dashboards.ActiveDashboardItem = value; 
                NotifyOfPropertyChange(()=>ActiveDashboardItem);
                
                
            }
        }
        

        public BindableCollection<Dashboard> Dashboards {get { return AppState.Dashboards; }}  

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            AppState.ScriptCommand += AppState_ScriptCommand;
            AppState.SensorSets.CollectionChanged += (e, f) =>
            {
                NotifyOfPropertyChange(() => SelectedSensorSets);
                NotifyOfPropertyChange(() => Category);
            };
            AppState.Dashboards.ActiveDashboardItemChanged +=
                (e, f) =>
                {
                    NotifyOfPropertyChange(() => ActiveDashboardItem);
                    if (ActiveDashboardItem.ViewModel != null && ActiveDashboardItem.ViewModel.ConfigScreen != null)
                    {
                        var b = ViewLocator.LocateForModel(ActiveDashboardItem.ViewModel.ConfigScreen, null, null) as FrameworkElement;
                        if (b == null) return;
                        b.HorizontalAlignment = HorizontalAlignment.Stretch;
                        b.VerticalAlignment = VerticalAlignment.Stretch;
                        ViewModelBinder.Bind(ActiveDashboardItem.ViewModel.ConfigScreen, b, null);
                        ((DataSetsView)view).ItemConfigScreen.Content = b;

                        //ItemConfigScreen = ActiveDashboardItem.ViewModel.ConfigScreen;
                    }
                };
            Plugin.GenerateDashboards();
        }

        void AppState_ScriptCommand(object sender, string command)
        {
            if (command == "FocusActiveDashboardItem")
            {
                SelectedTab = 3;
                AppState.ExpandLeftTab();
            }
        }


        public bool CanNextCategory { get { return SensorSets != null && SensorSets.Count > 1; } }
        
        public bool CanPreviousCategory { get { return SensorSets != null && SensorSets.Count > 1; } }

        public void PreviousCategory() {
            selectedSensorSetIndex--;
            if (selectedSensorSetIndex < 0) selectedSensorSetIndex = SensorSets.Count - 1; 
            NotifyOfPropertyChange(() => SelectedSensorSets);
            NotifyOfPropertyChange(() => Category);
        }

        public void NextCategory() {
            selectedSensorSetIndex++;
            if (selectedSensorSetIndex >= SensorSets.Count) selectedSensorSetIndex = 0;
            NotifyOfPropertyChange(() => SelectedSensorSets);
            NotifyOfPropertyChange(() => Category);
        }
    }
}