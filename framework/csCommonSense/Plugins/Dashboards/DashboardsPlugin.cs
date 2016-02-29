using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Caliburn.Micro;
using csCommon.Plugins.DashboardPlugin;
using csCommon.Utils.Collections;
using csEvents.Sensors;
using csShared;
using csShared.Controls.SlideTab;
using csShared.Interfaces;
using csShared.TabItems;


namespace csCommon.Plugins.DashboardPlugin
{
    [Export(typeof(IPlugin))]
    public class DashboardsPlugin : PropertyChangedBase, IPlugin
    {
        public bool CanStop { get { return true; } }

        private ISettingsScreen settings;

        public ISettingsScreen Settings
        {
            get { return settings; }
            set { settings = value; NotifyOfPropertyChange(() => Settings); }
        }

        private IPluginScreen screen = new DashboardsViewModel();

        public IPluginScreen Screen
        {
            get { return screen; }
            set { screen = value; NotifyOfPropertyChange(() => Screen); }
        }

        private bool hideFromSettings;

        public bool HideFromSettings
        {
            get { return hideFromSettings; }
            set { hideFromSettings = value; NotifyOfPropertyChange(() => HideFromSettings); }
        }

        public int Priority
        {
            get { return 1; }
        }

        public string Icon
        {
            get { return @"icons\globe.png"; }
        }

        private bool isRunning;

        public bool IsRunning
        {
            get { return isRunning; }
            set { isRunning = value; NotifyOfPropertyChange(() => IsRunning); }
        }

        public AppStateSettings AppState { get; set; }


        public string Name
        {
            get { return "DashboardPlugin"; }
        }

        public void Init()
        {       
            
        }

        

        private StartPanelTabItem spti;

        readonly DispatcherTimer updateTimer = new DispatcherTimer();

        public void Start()
        {
            IsRunning = true;
            
            var viewModel = new DataSetsViewModel() {Plugin = this};
            ((DashboardsViewModel)screen).Plugin = this;

            spti = new StartPanelTabItem
            {
                ModelInstance = viewModel,
                Position = StartPanelPosition.left,
                HeaderStyle = TabHeaderStyle.Image,
                Name = "Dashboards",
                Image = new BitmapImage(new Uri(@"pack://application:,,,/csCommon;component/Resources/Icons/chart2.png", UriKind.RelativeOrAbsolute))
            };

            AppState.AddStartPanelTabItem(spti);

            updateTimer.Interval = new TimeSpan(0, 0, 0, 0, 250);
            updateTimer.Tick += UpdateSensorSets;
            updateTimer.Start();

            Application.Current.MainWindow.KeyDown += MainWindow_KeyDown;
        }

        void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var d = AppState.Dashboards;
            if (e.Key == Key.F1 && d.Count>0) d.ActiveDashboard = d[0];

            if (e.Key == Key.F2 && d.Count > 1) d.ActiveDashboard = d[1];
            if (e.Key == Key.F3 && d.Count > 2) d.ActiveDashboard = d[2];
            if (e.Key == Key.F4 && d.Count > 3) d.ActiveDashboard = d[3];
            if (e.Key == Key.F5 && d.Count > 4) d.ActiveDashboard = d[4];

        }

        public void GenerateDashboards() // REVIEW TODO fix: Async removed
        {
            InitToolbox();
            AppState.Dashboards.Load("dashboards.config");
            AppState.Dashboards.CheckForEmptyDashboard();
            AppState.Dashboards.ActiveDashboard = AppState.Dashboards.GetDefault();

        }

        public void InitToolbox()
        {
            var tb = AppState.Dashboards.Toolbox;
            tb.Add(new DashboardItem()
            {
                Title = "Focus Time",
                Type = "csCommon.Plugins.Dashboards.Toolbox.FocusTime.FocusTimeDashboardViewModel",
                Config = ""      
            });

            tb.Add(new DashboardItem()
            {
                Title = "Map",
                Type = "csCommon.MapPlugins.EsriMap.EsriMapViewModel",
                Config = ""
            });

            tb.Add(new DashboardItem()
            {
                Title = "Events",
                Type = "csCommon.Plugins.Dashboards.Toolbox.Events.EventsDashboardViewModel",
                Config = "LastEvent"
            });
        }

        private DateTime lastFocusTime;

        void UpdateSensorSets(object sender, EventArgs e)
        {
            if (lastFocusTime == AppState.TimelineManager.FocusTime) return;
            lastFocusTime = AppState.TimelineManager.FocusTime;
            foreach (var ds in AppState.SensorSets.SelectMany(ss => ss))
                ds.SetFocusDate(AppState.TimelineManager.FocusTime);
        }

        public void Pause()
        {
            IsRunning = false;
            
        }

        public void Stop()
        {
            if (Application.Current.MainWindow!=null)
                Application.Current.MainWindow.KeyDown -= MainWindow_KeyDown;
        
            IsRunning = false;
            AppState.RemoveStartPanelTabItem(spti);
            updateTimer.Stop();
        }
    }
}
