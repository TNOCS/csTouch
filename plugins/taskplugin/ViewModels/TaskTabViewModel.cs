using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows;
using System.Windows.Data;
using Caliburn.Micro;
using csCommon.csMapCustomControls.CircularMenu;
using DataServer;
using TasksPlugin.Interfaces;
using csImb;
using csShared;
using csShared.FloatingElements;
using Media = DataServer.Media;

namespace TasksPlugin.ViewModels
{
    [Export(typeof(ITaskTab)), PartCreationPolicy(CreationPolicy.Shared)]
    public class TaskTabViewModel : Screen, ITaskTab {
        private DataServer.PoiService service;
        private Task selectedTask;
        public TaskPlugin Plugin { get; set; }

        public TaskTabViewModel() {
            Tasks = new CollectionViewSource();

        }

        protected override void OnViewLoaded(object view) {
            base.OnViewLoaded(view);

            var nav = new CircularMenuItem()
            {
                Icon = "pack://application:,,,/csTasksPlugin;component/Images/new task.png"
                //Element = "M93.621 44.371C93.621 20.318 74.053 0.75 50 0.75S6.379 20.318 6.379 44.371c0 19.396 12.727 35.867 30.266 41.521   L50 99.25l13.355-13.355C80.895 80.238 93.621 63.768 93.621 44.371z M50 76.736c-17.846 0-32.364-14.521-32.364-32.365   c0-17.845 14.519-32.364 32.364-32.364s32.364 14.519 32.364 32.364C82.364 62.216 67.846 76.736 50 76.736z M49.41954,39.667637C48.692036,39.667637 48.101837,40.270634 48.101837,41.014027 48.101837,41.76012 48.692036,42.362915 49.41954,42.362915 50.145443,42.362915 50.735249,41.76012 50.735249,41.014027 50.735249,40.270634 50.145443,39.667637 49.41954,39.667637z M44.825417,39.607838C44.097614,39.607838 43.509415,40.210533 43.509415,40.953926 43.509415,41.69762 44.097614,42.303116 44.825417,42.303116 45.55172,42.303116 46.141525,41.69762 46.141525,40.953926 46.141525,40.210533 45.55172,39.607838 44.825417,39.607838z M6.3522615,5.5287595C5.911829,5.5287595,5.5541072,5.8554168,5.5541077,6.2604427L5.5541077,37.508957C5.5541072,37.913853,5.911829,38.240654,6.3522615,38.240654L55.388371,38.240654C55.828773,38.240654,56.188175,37.913853,56.188175,37.508957L56.188175,6.2604427C56.188175,5.8554168,55.828773,5.5287595,55.388371,5.5287595z M0.98016852,0L61.087196,0C61.629902,-2.3297311E-09,62.068005,0.436275,62.068005,0.97923898L62.068005,42.791309C62.068005,43.333004,61.629902,43.772999,61.087196,43.772999L38.711491,43.772999 38.711491,42.46711C38.711491,41.924217,38.272388,41.484024,37.729683,41.484024L24.010818,41.484024C23.471514,41.484024,23.030712,41.924217,23.030712,42.46711L23.030712,43.772999 0.98016852,43.772999C0.43945515,43.772999,-3.5527137E-15,43.333004,0,42.791309L0,0.97923898C-3.5527137E-15,0.436275,0.43945515,-2.3297311E-09,0.98016852,0z",
                //Items = new List<CircularMenuItem>()
            };
            nav.Selected += (sender, args) => AddTaskWithScreenshot();

            AppStateSettings.Instance.CircularMenus.Add(nav);
        }

        public CollectionViewSource Tasks { get; set; }

        public DataServer.PoiService Service {
            get { return service; }
            set {
                if (service == value) return;
                service = value;
                if (service != null) {
                    Tasks.Source = ServiceTasks;
                    Tasks.View.Refresh();
                }
                NotifyOfPropertyChange(() => Service);
                NotifyOfPropertyChange(() => Tasks);
            }
        }


        private bool filterTasks;
        public bool FilterTasks {
            get { return filterTasks; }
            set {
                if (filterTasks == value) return;
                filterTasks = value;
                if (filterTasks) 
                    Tasks.View.Filter = o => {
                        var task = o as Task;
                        if (task == null) return true;
                        var informMe = false;
                        if (task.Labels.ContainsKey("InformMe")) bool.TryParse(task.Labels["InformMe"], out informMe);
                        return informMe;
                    };
                else
                    Tasks.View.Filter = null;

                NotifyOfPropertyChange(() => FilterTasks);
            }
        }

        public void AddTask()
        {
            var id = Guid.NewGuid().ToString();
            var task = new Task { Title = "New RFI", Date = DateTime.UtcNow };
            var taskDetailsViewModel = new TaskDetailsViewModel { Task = task, Service = Service, Plugin = Plugin, FloatingElementId = id};
            var size = new Size(300, 500);

            var fe = FloatingHelpers.CreateFloatingElement("RFI", new Point(300, 300), size, taskDetailsViewModel);
            fe.Id = id;
            AppStateSettings.Instance.FloatingItems.AddFloatingElement(fe);
        }

        private void AddTaskWithScreenshot()
        {
            if (Service == null) return;
            var id2 = Guid.NewGuid().ToString();
            var task = new Task { Title = "New RFI", Date = DateTime.UtcNow };
            var taskDetailsViewModel = new TaskDetailsViewModel { Task = task, Service = Service, Plugin = Plugin, FloatingElementId = id2 };
            var size = new Size(300, 500);
            
            var id = Guid.NewGuid() + ".png";
            
            
                var media = new Media
                {
                    Id = id,
                    LocalPath = Service.store.GetLocalUrl(Service.Folder, id),
                    Type = MediaType.Photo,
                    Title = task.Title
                };
            
            Screenshots.SaveImageOfControl(AppStateSettings.Instance.ViewDef.MapControl, media.LocalPath);
            //media.Image = Screenshot as BitmapSource;
            foreach (var m in task.AllMedia)
            {
                try { if (File.Exists(m.LocalPath)) File.Delete(m.LocalPath); }
                catch (Exception) { }
            }
            task.AllMedia.Clear();
            task.AllMedia.Add(media);
            //await Service.store.SaveBytes(id, media.ByteArray);
   
            var fe = FloatingHelpers.CreateFloatingElement("RFI", new Point(300, 300), size, taskDetailsViewModel);
            fe.Id = id2;
            AppStateSettings.Instance.FloatingItems.AddFloatingElement(fe);
        }

        public ContentList ServiceTasks {
            get { return service == null ? null : service.Tasks; }
        }

        public Task SelectedTask {
            get { return selectedTask; }
            set {
                if (selectedTask == value) return;
                selectedTask = value;
                DownloadMedia(selectedTask);
                NotifyOfPropertyChange(() => SelectedTask);
            }
        }

        private void DownloadMedia(BaseContent task) {
            if (task == null || task.AllMedia.Count == 0) return;
            foreach (var media in task.AllMedia) {
                var store = service.store;
                media.LocalPath = store.GetLocalUrl(service.Folder, media.Id);
                if (!store.HasFile(media.LocalPath)) {
                    service.RequestData(media.Id, MediaReceived);
                }
            }
        }

        private static void MediaReceived(string contentid, byte[] content, Service service1) {
            
        }

        public void OpenMenu() {
            var taskDetailsViewModel = new TaskDetailsViewModel { Task = selectedTask, Service = Service, Plugin = Plugin, IsTaskSent = true };
            var fe = FloatingHelpers.CreateFloatingElement("RFI", new Point(300, 300), new Size(300, 500), taskDetailsViewModel);
            taskDetailsViewModel.FloatingElementId = fe.Id;
            AppStateSettings.Instance.FloatingItems.AddFloatingElement(fe);
        }

    }
}
