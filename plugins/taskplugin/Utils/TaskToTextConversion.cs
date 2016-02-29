using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using DataServer;
using TasksPlugin.DAL;

namespace TasksPlugin.Utils {
    public class TaskToTextConversion {
        private const string DefaultTaskTemplate =
            @"Opdracht {Title}

{Description}

Namens {Signature}
{TimeStamp:yyyy.MM.dd HH:mm}

===================================================================================================

";

        private const string DefaultTaskResponseTemplate =
            @"{Tag}

{TagResponse}

";

        private const string RecipientsKey = "Recipients";
        private const string Title = "{0}", Description = "{1}", Signature = "{2}", Time = "3", TagResponses = "{4}", Map="{5}", Tag = "{0}", TagResponse = "{1}";
        private readonly TaskSettings taskSettings = TaskSettings.Instance;
        private DataServer.PoiService service;
        private string taskResponseTemplate;
        private string taskTemplate;
        private readonly bool saveAsHtml;

        public TaskToTextConversion() {
            saveAsHtml = taskSettings.SaveAsHtml;
            InitializeSavingTasks();
        }

        public DataServer.PoiService Service {
            get { return service; }
            set {
                if (service == value) return;
                service = value;
                if (service == null) return;
                service.Tasks.CollectionChanged += TasksCollectionChanged;
                foreach (var task in service.Tasks.OfType<Task>()) {
                    task.PropertyChanged += TaskPropertyChanged;
                    UpdateTaskStatus(task);
                    ThreadPool.QueueUserWorkItem(delegate { SaveTaskToFolder(task); });
                }
            }
        }

        private void InitializeSavingTasks() {
            var taskTemplateFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "Tasks", saveAsHtml ? "TaskHtmlTemplate.txt": "TaskTemplate.txt");
            try {
                taskTemplate = File.ReadAllText(taskTemplateFile);
            }
            catch {
                taskTemplate = DefaultTaskTemplate;
            }
            finally {
                taskTemplate = taskTemplate.Replace("{Title}", Title)
                                           .Replace("{Description}", Description)
                                           .Replace("{Signature}", Signature)
                                           .Replace("{TagResponses}", TagResponses)
                                           .Replace("{Map}", Map)
                                           .Replace("TimeStamp", Time);
            }
            var taskResponseTemplateFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "Tasks",  saveAsHtml ? "TaskHtmlResponseTemplate.txt" : "TaskResponseTemplate.txt");
            try {
                taskResponseTemplate = File.ReadAllText(taskResponseTemplateFile);
            }
            catch {
                taskResponseTemplate = DefaultTaskResponseTemplate;
            }
            finally {
                taskResponseTemplate = taskResponseTemplate.Replace("{Tag}", Tag).Replace("{TagResponse}", TagResponse);
            }
        }

        private void TaskPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (!taskSettings.IsSavingTasksToOutputFolder || !Directory.Exists(taskSettings.OutputFolderForTasks)) return;
            if (!e.PropertyName.Equals("Labels")) return;
            var task = sender as Task;
            if (task != null) SaveTaskToFolder(task);
        }

        private void SaveTaskToFolder(Task task) {
            if (!taskSettings.IsSavingTasksToOutputFolder || !Directory.Exists(taskSettings.OutputFolderForTasks)) return;
            var signature = task.Labels.ContainsKey("Signature")
                                ? task.Labels["Signature"]
                                : string.Empty;
            var recipients = !task.Labels.ContainsKey("Recipients")
                                 ? string.Empty
                                 : task.Labels["Recipients"];
            var fileName = Path.Combine(taskSettings.OutputFolderForTasks,
                                        string.Format("{0:yyyy-MM-dd HHmm} no{1:000} {2} {3}.{4}", task.Date, service.Tasks.IndexOf(task) + 1,
                                        signature, recipients.Replace(", ", "-"), saveAsHtml ? "html" : "txt"));

            var responseText = new StringBuilder();
            var tags = recipients.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var tag in tags) {
                var key = tag.Trim().Replace(' ', '_');
                var response = task.Labels.ContainsKey(key)
                                   ? task.Labels[key]
                                   : string.Empty;
                responseText.AppendLine(string.Format(taskResponseTemplate, tag.Trim(), response));
            }
            try {
                var imageFileName = Path.ChangeExtension(fileName, "png");
                if (task.AllMedia.Count > 0) {
                    var media = task.AllMedia[0];
                    if (!File.Exists(media.LocalPath)) DownloadMedia(task);
                    if (File.Exists(media.LocalPath) && !File.Exists(imageFileName)) File.Copy(media.LocalPath, imageFileName, true);
                }
                var myImage = File.Exists(imageFileName)
                                  ? Path.GetFileName(imageFileName)
                                  : string.Empty;
                var taskAsText = string.Format(taskTemplate, task.Title, task.Description, signature, task.Date, responseText, myImage);
                File.WriteAllText(fileName, taskAsText, new UTF8Encoding());
            }
            catch (SystemException e) {
                Console.WriteLine(e.Message);
            }
        }

        private void TasksCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    foreach (var task in e.NewItems.OfType<Task>()) {
                        SaveTaskToFolder(task);
                        task.PropertyChanged += TaskPropertyChanged;
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                        foreach (var task in e.OldItems.OfType<Task>())
                            task.PropertyChanged -= TaskPropertyChanged;
                    break;
            }
        }

        private static void UpdateTaskStatus(Task task) {
            if (task == null || !task.Labels.ContainsKey(RecipientsKey)) return;
            var status = TaskState.None;
            var isFinished = true;
            foreach (var recipient in task.Labels[RecipientsKey].Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)) {
                if (task.Labels.ContainsKey(recipient) && !string.IsNullOrEmpty(task.Labels[recipient])) status = TaskState.Inprogress;
                else isFinished = false;
            }
            task.State = isFinished ? TaskState.Finished : status;
        }

        private void DownloadMedia(BaseContent task) {
            if (task == null || task.AllMedia.Count == 0) return;
            foreach (var media in task.AllMedia) {
                var store = service.store;
                media.LocalPath = store.GetLocalUrl(service.Folder, media.Id);
                if (!store.HasFile(media.LocalPath))
                    service.RequestData(media.Id, null);
            }
        }

    }
}