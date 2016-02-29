using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using Caliburn.Micro;
using TasksPlugin.DAL;
using TasksPlugin.Interfaces;
using csShared.FloatingElements;

namespace TasksPlugin.ViewModels {
    [Export(typeof(ITaskConfiguration)), PartCreationPolicy(CreationPolicy.Shared)]
    public class TaskConfigurationViewModel : PropertyChangedBase, ITaskConfiguration {
        private const string Separator = "\r\n";
        private readonly TaskSettings taskSettings = TaskSettings.Instance;
        private string right;
        private string left;
        private string top;
        private string bottom;

        public TaskConfigurationViewModel() {
            right = string.Join(Separator, taskSettings.RecipientsRight);
            left = string.Join(Separator, taskSettings.RecipientsLeft);
            top = string.Join(Separator, taskSettings.RecipientsTop);
            bottom = string.Join(Separator, taskSettings.RecipientsBottom);
        }

        public bool IsTasksServer {
            get { return taskSettings.IsTasksServer; }
            set {
                if (taskSettings.IsTasksServer == value) return;
                taskSettings.IsTasksServer = value;
                NotifyOfPropertyChange(() => IsTasksServer);
            }
        }

        public string Right {
            get { return right; }
            set {
                if (string.Equals(right, value)) return;
                right = value;
                ResetCollection(taskSettings.RecipientsRight, right);
                taskSettings.RecipientsRight = new ObservableCollection<string>(right.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries).ToList());
                NotifyOfPropertyChange(() => Right);
            }
        }

        public string Left {
            get { return left; }
            set {
                if (string.Equals(left, value)) return;
                left = value;
                ResetCollection(taskSettings.RecipientsLeft, left);
                taskSettings.RecipientsLeft = new ObservableCollection<string>(left.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries).ToList());
                NotifyOfPropertyChange(() => Left);
            }
        }

        public string Top {
            get { return top; }
            set {
                if (string.Equals(top, value)) return;
                top = value;
                ResetCollection(taskSettings.RecipientsTop, top);
                NotifyOfPropertyChange(() => Top);
            }
        }

        public string Bottom
        {
            get { return bottom; }
            set {
                if (string.Equals(bottom, value)) return;
                bottom = value;
                ResetCollection(taskSettings.RecipientsBottom, bottom);
                NotifyOfPropertyChange(() => Bottom);
            }
        }

        private void ResetCollection(ObservableCollection<string> oldRecipients, string newRecipients) {
            oldRecipients.Clear();
            var recipients = new ObservableCollection<string>(newRecipients.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries).ToList());
            foreach (var recipient in recipients) oldRecipients.Add(recipient);
        }

        public string FloatingElementId { get; set; }

        public bool IsSavingTasksToOutputFolder {
            get { return taskSettings.IsSavingTasksToOutputFolder;  }
            set {
                if (taskSettings.IsSavingTasksToOutputFolder == value) return;
                taskSettings.IsSavingTasksToOutputFolder = value;
                NotifyOfPropertyChange(() => IsSavingTasksToOutputFolder);
            }
        }

        public string OutputFolderForTasks {
            get { return taskSettings.OutputFolderForTasks; }
            set { 
                if (string.Equals(taskSettings.OutputFolderForTasks, value)) return;
                taskSettings.OutputFolderForTasks = value;
                NotifyOfPropertyChange(() => OutputFolderForTasks);
            }
        }

        public bool SaveAsHtml {
            get { return taskSettings.SaveAsHtml; }
            set { taskSettings.SaveAsHtml = value; }
        }

        public void OK() {
            taskSettings.Save();
            Close();
        }

        public void Cancel() {
            Close();
        }

        private void Close() { FloatingHelpers.RemoveFloatingElement(FloatingElementId); } 
    }
}