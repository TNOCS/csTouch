using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using DataServer;
using TasksPlugin.DAL;
using csImb;
using csShared;
using csShared.Documents;
using csShared.FloatingElements;
using csShared.Utils;
using Media = DataServer.Media;

namespace TasksPlugin.ViewModels
{
    public interface ITaskDetails
    {
    }


    /// <summary>
    ///   Interaction logic for twowaysliding.xaml
    /// </summary>
    [Export(typeof (ITaskDetails))]
    public class TaskDetailsViewModel : Screen
    {
        private const string RecipientsKey = "Recipients";
        private const string SignatureKey = "Signature";
        private const string InformMeKey = "InformMe";
        private string floatingElementId;
        private TaskPlugin plugin;
        //private bool showResponses;

        public TaskDetailsViewModel()
        {
            Responses = new ObservableCollection<ResponseViewModel>();
        }
        
        public TaskPlugin Plugin
        {
            get { return plugin; }
            set {
                plugin = value; 
                NotifyOfPropertyChange(()=>Plugin);
            }
        }
        
        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public string FloatingElementId
        {
            get { return floatingElementId; }
            set
            {
                if (string.Equals(floatingElementId, value)) return;
                floatingElementId = value;
                NotifyOfPropertyChange(() => FloatingElementId);
            }
        }

        private Task task;

        public Task Task
        {
            get { return task; }
            set {
                task = value;
                UpdateResponses();
                NotifyOfPropertyChange(() => Task);
                NotifyOfPropertyChange(() => Title);
                NotifyOfPropertyChange(() => Screenshot);
                NotifyOfPropertyChange(() => Description);
            }
        }

        private DataServer.PoiService service;
        private ImageSource screenshot;

        public DataServer.PoiService Service
        {
            get { return service; }
            set {
                service = value; 
                NotifyOfPropertyChange(()=>Service);
            }
        }

        public string Title {
            get { return task.Title; }
            set {
                task.Title = value;
                NotifyOfPropertyChange(() => Title);
                NotifyOfPropertyChange(() => CanSendTask);
            }
        }

        public string Description {
            get {
                return task.Description;
            } 
            set {
                task.Description= value;
                NotifyOfPropertyChange(() => Description);
            }
        }

        //public bool ShowResponses {
        //    get { return showResponses; }
        //    set {
        //        if (showResponses == value) return;
        //        showResponses = value;
        //        NotifyOfPropertyChange(() => ShowResponses);
        //    }
        //}

        public string Signature {
            get
            {
                if (!task.Labels.ContainsKey(SignatureKey))
                    task.Labels[SignatureKey] = TaskSettings.Instance.Signature;
                return task.Labels[SignatureKey];
            }
            set {
                task.Labels[SignatureKey] = TaskSettings.Instance.Signature = value;
                NotifyOfPropertyChange(() => Signature);
                NotifyOfPropertyChange(() => CanSendTask);
            }
        }

        public bool CanSendTask { get { return !IsTaskSent && !string.IsNullOrEmpty(Title) && !string.IsNullOrEmpty(Signature); } }

        public bool IsTaskSent { get; set; }

        public ImageSource Screenshot {
            get {
                if (screenshot == null) GetScreenshot();
                return screenshot;
            }
            set {
                if (Equals(screenshot, value)) return;
                screenshot = value;
                NotifyOfPropertyChange(() => Screenshot);
            }
        }

        private void GetScreenshot()
        {
            if (task == null || task.AllMedia.Count == 0) return;
            foreach (var media in task.AllMedia)
            {
                var store = service.store;
                media.LocalPath = store.GetLocalUrl(service.Folder, media.Id);
                if (!store.HasFile(media.LocalPath))
                {
                    service.RequestData(media.Id, MediaReceived);
                }
                else
                {
                    screenshot = new BitmapImage(new Uri(media.LocalPath));
                }
            }
        }

        public void MediaReceived(string contentId, byte[] content, Service aService) {
            SetImage(content);
        }

        private void SetImage(byte[] byteArray)
        {
            Execute.OnUIThread(() =>
            {
                try
                {
                    if (byteArray == null || byteArray.Length == 0) return;
                    var image = new BitmapImage();
                    using (var byteStream = new MemoryStream(byteArray))
                    {
                        image.BeginInit();
                        image.StreamSource = byteStream;
                        image.EndInit();
                    }
                    screenshot = image;
                    NotifyOfPropertyChange(() => Screenshot);
                }
                catch (Exception ex)
                {
                    Logger.Log("MediaItem", "Error reading image", ex.Message, Logger.Level.Error);
                }
            });
        }

        //private ImageSource GetScreenshot() {
        //    if (task == null || task.AllMedia.Count == 0) return null;
        //    var media = task.AllMedia[0];
        //    media.LoadPhoto(Service);
        //    if (media.Image == null) media.PropertyChanged += MediaOnPropertyChanged;
        //    return media.Image;
        //}

        //private void MediaOnPropertyChanged(object sender, PropertyChangedEventArgs e) {
        //    if (!e.PropertyName.Equals("Image")) return;
        //    var media = sender as Media;
        //    if (media == null) return;
        //    media.PropertyChanged -= MediaOnPropertyChanged;
        //    screenshot = media.Image;
        //    NotifyOfPropertyChange(() => Screenshot);
        //}

        public bool InformMe {
            get {
                var informMe = false;
                if (task.Labels.ContainsKey(InformMeKey))
                    bool.TryParse(task.Labels[InformMeKey], out informMe);
                return informMe;
            }
            set {
                task.Labels[InformMeKey] = value.ToString();
                NotifyOfPropertyChange(() => InformMe);
            }
        }

        public bool CanTakeScreenshotMap {
            get { return !IsTaskSent; }
        }

        public void TakeScreenshotMap() {
            var id = Guid.NewGuid() + ".png";
            var media = new Media {
                Id = id,
                LocalPath = Service.store.GetLocalUrl(Service.Folder, id),
                Type = MediaType.Photo,
                Title = task.Title
            };
            Screenshot = Screenshots.SaveImageOfControl(AppState.ViewDef.MapControl, media.LocalPath);
            //media.Image = Screenshot as BitmapSource;
            foreach (var m in task.AllMedia) {
                try { if (File.Exists(m.LocalPath)) File.Delete(m.LocalPath); }
                catch (Exception) {}
            }
            task.AllMedia.Clear(); 
            task.AllMedia.Add(media);
            //await Service.store.SaveBytes(id, media.ByteArray);
            AppState.TriggerNotification("Map Screenshot saved.");            
        }

        public void OpenScreenshot() {
            if (task == null || task.AllMedia.Count == 0) return;
            var media = task.AllMedia[0];
            if (string.IsNullOrEmpty(media.LocalPath))
                media.LocalPath = Service.store.GetLocalUrl(Service.Folder, media.Id);
            var d = new Document { Location = media.LocalPath };
            var fed = FloatingHelpers.CreateFloatingElement(d);
            fed.Title = task.Title;
            fed.CanFullScreen = true;
            fed.StartPosition = new Point(500, 300);
            AppStateSettings.Instance.FloatingItems.AddFloatingElement(fed);
        }

        public void DeleteTask() {
            Service.Tasks.Remove(task);
            Service.SaveXml();
            FloatingHelpers.RemoveFloatingElement(FloatingElementId);
            AppState.TriggerNotification("Task deleted.");
        }

        private void UpdateResponses() {
            Responses.Clear();
            if (task == null || !task.Labels.ContainsKey(RecipientsKey)) return;
            UpdateTaskStatus();
            foreach (var recipient in task.Labels[RecipientsKey].Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)) {
                var responseViewModel = new ResponseViewModel(recipient);
                var recipientKey = recipient.Trim().Replace(' ', '_');
                if (task.Labels.ContainsKey(recipientKey)) responseViewModel.Response = task.Labels[recipientKey];
                responseViewModel.PropertyChanged += (sender, args) => {
                    task.Labels[responseViewModel.LabelKey] = responseViewModel.Response;
                    if (service.IsLocal) service.SaveXml();
                    task.NotifyOfPropertyChange("Labels");
                    UpdateTaskStatus();
                };
                Responses.Add(responseViewModel);
            }
        }

        private void UpdateTaskStatus() {
            if (task == null || !task.Labels.ContainsKey(RecipientsKey)) return;
            var status = TaskState.None;
            var isFinished = true;
            foreach (var recipient in task.Labels[RecipientsKey].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var recipientKey = recipient.Trim().Replace(' ', '_');
                if (task.Labels.ContainsKey(recipientKey) && !string.IsNullOrEmpty(task.Labels[recipientKey])) status = TaskState.Inprogress;
                else isFinished = false;
            }
            task.State = isFinished ? TaskState.Finished : status;
        }

        public ObservableCollection<ResponseViewModel> Responses { get; set; } 

        /// <summary>
        /// Simple view model to retreive the responses.
        /// </summary>
        public class ResponseViewModel : PropertyChangedBase {
            private string response;

            public ResponseViewModel(string recipient) {
                Recipient = recipient;
                LabelKey = recipient.Trim().Replace(' ', '_');
            }

            public string Recipient { get; set; }

            public string LabelKey { get; set; }

            public string Response {
                get { return response; }
                set {
                    response = value;
                    NotifyOfPropertyChange(()=> Response);
                }
            }
        }
    }
}