using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using Caliburn.Micro;
using DataServer;
using Microsoft.Surface.Presentation;
using TasksPlugin.DAL;
using TasksPlugin.Views;
using csShared;
using csShared.Interfaces;

namespace TasksPlugin.ViewModels
{
    public interface ITaskTargets {}

    /// <summary>
    ///   Responsible for showing the border with all potential RFI recipients.
    /// </summary>
    [Export(typeof (ITaskTargets))]
    public class TaskTargetsViewModel : Screen, IPluginScreen
    {
        private const string RecipientsKey = "Recipients";
        private readonly TaskSettings taskSettings = TaskSettings.Instance;
        private readonly List<Border> visitedBorders = new List<Border>();
        private TaskPlugin plugin;
        private FloatingElement floatingElement;
        private TaskDragItem taskDragItem;
        private Task activeTask;
        private bool showConfirmation;
        private string lead;
        private string followers;

        public TaskTargetsViewModel() {
            taskSettings.PropertyChanged += (sender, args) => {
                if (!args.PropertyName.Equals("HasTargetCollectionChanged")) return;
                NotifyOfPropertyChange(() => RecipientsTop);
                NotifyOfPropertyChange(() => RecipientsBottom);
                // TODO Left and right do not update properly.
                NotifyOfPropertyChange(() => RecipientsLeft);
                NotifyOfPropertyChange(() => RecipientsRight);
            };
        }

        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public TaskPlugin Plugin
        {
            get { return plugin; }
            set { plugin = value; NotifyOfPropertyChange(()=>Plugin); }
        }
        
        public string Name { get { return "Targets"; }  }

        public ObservableCollection<string> RecipientsTop { get { return taskSettings.RecipientsTop; } }
        public ObservableCollection<string> RecipientsBottom { get { return taskSettings.RecipientsBottom; } }
        public ObservableCollection<string> RecipientsLeft { get { return taskSettings.RecipientsLeft; } }
        public ObservableCollection<string> RecipientsRight { get { return taskSettings.RecipientsRight; } }

        public void DragEnter(object sender, SurfaceDragDropEventArgs e) {
            var border = sender as Border;
            if (border == null) return;
            if (visitedBorders.Contains(border))
            {
                visitedBorders.Remove(border);
                border.Background = Brushes.Yellow;
            }
            else
            {
                visitedBorders.Insert(0, border);
                border.Background = Brushes.Red;
            }
        }

        public void Drop(object sender, SurfaceDragDropEventArgs e) {
            var border = sender as Border;
            if (border == null) return;
            border.Background = Brushes.Red;
            if (!visitedBorders.Contains(border))
                visitedBorders.Add(border);

            taskDragItem = e.Cursor.Data as TaskDragItem;
            if (taskDragItem == null || taskDragItem.Service == null || taskDragItem.Task == null) return;
            activeTask = taskDragItem.Task;

            Lead = Followers = string.Empty;

            if (visitedBorders.Count > 0) {
                Lead = (string) visitedBorders[0].DataContext;

                if (visitedBorders.Count > 1) {
                    var sb = new StringBuilder();
                    for (var i = 1; i < visitedBorders.Count; i++) {
                        var visitedBorder = visitedBorders[i];
                        sb.Append((string) visitedBorder.DataContext);
                        sb.Append(", ");
                    }
                    Followers = sb.ToString().TrimEnd(new[] {',', ' '});
                }
                else Followers = "-";
            }

            activeTask.Labels[RecipientsKey] = visitedBorders.Count <= 1 
                ? Lead
                : string.Format("{0}, {1}", Lead, Followers);
            ShowConfirmation = true;
            RemoveFloatingElement();
            e.Handled = true;
        }

        public string Lead {
            get { return lead; }
            set {
                if (string.Equals(lead, value)) return;
                lead = value;
                NotifyOfPropertyChange(() => Lead);
            }
        }

        public string Followers {
            get { return followers; }
            set {
                if (string.Equals(followers, value)) return;
                followers = value;
                NotifyOfPropertyChange(() => Followers);
            }
        }

        public void Cancel() {
            Plugin.ActiveTask.Labels[RecipientsKey] = string.Empty;
            if (floatingElement != null) AppStateSettings.Instance.FloatingItems.AddFloatingElement(floatingElement);
            Stop();
        }

        public void Stop() {
            ShowConfirmation = false;
            foreach (var border in visitedBorders)
                border.Background = Brushes.Yellow;
            visitedBorders.Clear();
            AppState.RestoreVisibleState();
            floatingElement = null;
            Plugin.ActiveTask = null;
        }

        private void RemoveFloatingElement() {
            floatingElement = AppState.FloatingItems.FirstOrDefault(f => string.Equals((string) f.Id, taskDragItem.FloatingElementId));
            if (floatingElement == null) return;
            AppState.FloatingItems.Remove(floatingElement);
        }

        public void Send() {
            var service = taskDragItem.Service;
            service.Tasks.Add(activeTask);
            //service.Tasks.RegisterContent(activeTask);
            //if (service.IsLocal)
            //{
            //    service.Tasks.RegisterContent(activeTask);
            //}
            //else if (!service.Tasks.Contains(activeTask))
            //    service.Tasks.Add(activeTask);

            if (activeTask.AllMedia.Count > 0)
                SendMediaToServer();
            if (service.IsLocal) service.SaveXml();
            Stop();
        }

        private void SendMediaToServer() {
            var service = taskDragItem.Service;
            var store = service.store;
            foreach (var media in activeTask.AllMedia)
            {
                media.LocalPath = store.GetLocalUrl(service.Folder, media.Id);
                Plugin.DataServer.SendFile(media.Id, File.ReadAllBytes(media.LocalPath), service);
            }
        }

        public bool ShowConfirmation {
            get { return showConfirmation; }
            set {
                if (showConfirmation == value) return;
                showConfirmation = value;
                NotifyOfPropertyChange(() => ShowConfirmation);
            }
        }

        public void DragDropCancelled(object sender, SurfaceDragDropEventArgs surfaceDragDropEventArgs) {
            foreach (var border in visitedBorders) border.Background = Brushes.Yellow;
            visitedBorders.Clear();
            Plugin.ActiveTask = null;
            AppState.RestoreVisibleState();
        }
    }

    
}