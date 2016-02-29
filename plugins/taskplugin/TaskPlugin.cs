using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using DataServer;
using TasksPlugin.Interfaces;
using TasksPlugin.Utils;
using TasksPlugin.ViewModels;
using csShared;
using csShared.Controls.SlideTab;
using csShared.FloatingElements;
using csShared.Geo;
using csShared.Interfaces;
using csShared.TabItems;
using Task = DataServer.Task;

//using Task = DataServer.Task;

// TODO BUG When there is no Tasks service running, make sure that you cannot use the Tasks tab.
// TODO Remove task tab items links en onderin (staat niets nuttigs in).
// TODO Add indication in tab that there is a new message (new task, completed task, open tasks).
// TODO Nice-to-have: Add confirmation or delay when deleting a task.
// TODO Nice-to-have: Add map coordinates to task: and allow the recipient to set the map's coordinates.

// TODO LATER  We gebruiken nu twee PoiService types. Migreer naar de laatste?
// NOTE I do not synchronize the recipients, so each end point can choose them themselves


namespace TasksPlugin
{
    [Export(typeof(IPlugin))]
    public class TaskPlugin : PropertyChangedBase, IPlugin
    {
        private readonly TaskToTextConversion taskToTextConversion = new TaskToTextConversion();
        private TaskTargetsViewModel screen;
        private ISettingsScreen settings;
        private bool hideFromSettings;
        private string file;
        private bool isRunning;
        private TaskTabViewModel tabViewModel;

        public IPluginScreen Screen
        {
            get { return screen; }
            set { screen = (TaskTargetsViewModel) value; NotifyOfPropertyChange(() => Screen); }
        }

        public bool CanStop { get { return true; } }


        public ISettingsScreen Settings
        {
            get { return settings; }
            set { settings = value; NotifyOfPropertyChange(() => Settings); }
        }


        public bool HideFromSettings
        {
            get { return hideFromSettings; }
            set { hideFromSettings = value; NotifyOfPropertyChange(()=>HideFromSettings); }
        }
        

        public int Priority
        {
            get { return 3; }
        }

        public string Icon
        {
            get { return @"icons\bookmarks.png"; }           
        }
        
        public string BFile
        {
            get { return file; }
            set { file = value; NotifyOfPropertyChange(()=>BFile); }
        }
        
        #region IPlugin Members

        public string Name
        {
            get { return "TasksPlugin"; }
        }

        public bool IsRunning
        {
            get { return isRunning; }
            set { isRunning = value; NotifyOfPropertyChange(() => IsRunning); }
        }

        public AppStateSettings AppState { get; set; }
        public FloatingElement Element { get; set; }
        
        private StartPanelTabItem TabItem { get; set; }

        public DataServerBase DataServer {
            get { return dataServer; }
            set {
                if (dataServer == value) return;
                dataServer = value;
                InitializeService();
                NotifyOfPropertyChange(() => DataServer);
            }
        }

        private async void InitializeService()
        {
            
            if (dataServer == null) return;
            // First try to subscribe to an existing task service
            SubscribeToTaskService(dataServer.Services);
            // If successful, stop
            if (tabViewModel.Service != null) return;
            // In case you need to start it, do so.
            if (DAL.TaskSettings.Instance.IsTasksServer) {
                tabViewModel.Service = OpenExistingTasksService() ?? await CreateNewTasksService();
                taskToTextConversion.Service = tabViewModel.Service;
                if (!AppState.StartPanelTabItems.Contains(TabItem)) AppState.AddStartPanelTabItem(TabItem);
            }
            else {
                // Wait for it to appear.
                dataServer.Services.CollectionChanged -= ServicesOnCollectionChanged;
                dataServer.Services.CollectionChanged += ServicesOnCollectionChanged;
            }
        }

        private PoiService service;
        private PoiService OpenExistingTasksService() {
            var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "Tasks");
            if (Directory.Exists(folder))
            {
                var referenceDataServiceFile = Directory.GetFiles(folder, "*.ds").LastOrDefault();
                if (string.IsNullOrEmpty(referenceDataServiceFile)) return null;
                service = dataServer.AddLocalDataService(folder, Mode.server, referenceDataServiceFile);
                dataServer.Subscribe(service);
                service.MakeOnline();
                return service;
            }
            return null;
        }

        private void ServicesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    SubscribeToTaskService(e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null) {
                        foreach (var s in e.OldItems.Cast<Service>().Where(s => s.Name.Contains("RFI"))) {
                            dataServer.UnSubscribe(s);
                            tabViewModel.Service = null;
                            AppState.RemoveStartPanelTabItem(TabItem);
                            break;
                        }
                    }
                    break;
            }
        }

        private void SubscribeToTaskService(IEnumerable services) {
            if (tabViewModel.Service != null) return; // Only subscribe to the first one.
            foreach (var service1 in services.Cast<Service>().Where(s => s.Name.Contains("RFI"))) {
                dataServer.Subscribe(service1, Mode.client);
                service1.Initialized += (sender, args) => Execute.OnUIThread(() => {
                    tabViewModel.Service = sender as PoiService;
                    taskToTextConversion.Service = tabViewModel.Service;
                    if (!AppState.StartPanelTabItems.Contains(TabItem)) AppState.AddStartPanelTabItem(TabItem);
                });
            }
        }

        private async Task<PoiService> CreateNewTasksService() {
            try {
                var referenceDataServiceFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "Tasks",
                                                            "RFI.dsd");
                if (dataServer == null || !File.Exists(referenceDataServiceFile)) return null;
                var folder = Path.GetDirectoryName(referenceDataServiceFile);
                service = global::DataServer.PoiService.GetCleanClone(
                    folder,
                    Path.GetFileName(referenceDataServiceFile),
                    folder,
                    Path.GetFileNameWithoutExtension(file), false);
                service.Name = "RFI";
                service.SaveXml();
                service = dataServer.AddLocalDataService(folder, Mode.server, referenceDataServiceFile);
                dataServer.Subscribe(service, Mode.server);
                service.MakeOnline();
                return service;
            }
            catch (SystemException e) {
                Console.WriteLine(e.Message);
                return null;
            }
        }


        public void Init() {
           
        }

        private void AddConfigMenu() {
            var configMenu = new MenuItem {
                Name = "Config RFI"
            };
            configMenu.Clicked += (e1, s1) => ShowConfigurationMenu();
            AppState.MainMenuItems.Add(configMenu);
        }

        private void ShowConfigurationMenu() {
            var vm = IoC.GetInstance(typeof (ITaskConfiguration), string.Empty) as TaskConfigurationViewModel;
            if (vm == null) return;
            var fe = FloatingHelpers.CreateFloatingElement("RFI Configuration", new Point(300, 300), new Size(600, 450), vm);
            vm.FloatingElementId = fe.Id;
            AppStateSettings.Instance.FloatingItems.AddFloatingElement(fe);
        }

        private Task activeTask;
        private DataServerBase dataServer;

        public Task ActiveTask
        {
            get { return activeTask; }
            set { activeTask = value; NotifyOfPropertyChange(() => ActiveTask); NotifyOfPropertyChange(() => IsTaskActive); }
        }

        public bool IsTaskActive
        {
            get { return ActiveTask != null; }
        }
        
        void ViewDefVisibleChanged(object sender, VisibleChangedEventArgs e)
        {
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (AppState.ViewDef.Visible && IsRunning)
            {
                if (!AppState.FloatingItems.Contains(Element)) AppState.FloatingItems.AddFloatingElement(Element);
            }
            else
            {
                if (AppState.FloatingItems.Contains(Element)) AppState.FloatingItems.RemoveFloatingElement(Element);
            }
        }

        public void GoTo()
        {

        }

        public void Start()
        {
            IsRunning = true;
            UpdateVisibility();

            Screen = new TaskTargetsViewModel { Plugin = this };
            var viewModel = (TasksViewModel)IoC.GetInstance(typeof(ITasks), "");
            if (viewModel != null)
            {
                tabViewModel = (TaskTabViewModel)IoC.GetInstance(typeof(ITaskTab), "");
                tabViewModel.Plugin = this;
                //tabViewModel.TaskTargetsViewModel = Screen as TaskTargetsViewModel;
                TabItem = new StartPanelTabItem
                {
                    Name = "RFI",
                    HeaderStyle = TabHeaderStyle.Image,
                    Image = new BitmapImage(new Uri("pack://application:,,,/csTasksPlugin;component/Images/TaskWhite.png")),
                    ModelInstance = tabViewModel,
                    Position = StartPanelPosition.left,
                };
                AppState.AddStartPanelTabItem(TabItem);
                //Element = FloatingHelpers.CreateFloatingElement("Screenshots", DockingStyles.Right, viewModel, Icon,Priority);
                //UpdateVisibility();
            }
            AddConfigMenu();
            AppState.ViewDef.VisibleChanged += ViewDefVisibleChanged;

            //Application.Current.MainWindow.PreviewKeyDown += MainWindow_PreviewKeyDown;
        }

        //private static void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        //{
        //    if (e.Key == Key.Snapshot || e.Key == Key.PrintScreen || e.Key == Key.End)
        //    {
        //        //Console.WriteLine("test");
        //    }
        //}

        public void Pause()
        {
            IsRunning = false;
            UpdateVisibility();
        }

        public void Stop()
        {
            IsRunning = false;
            if (dataServer!=null) dataServer.UnSubscribe(service);
            //try { File.Delete(service.FileName); }
            //catch { }

            UpdateVisibility();
        }

        #endregion
       
    }
}
