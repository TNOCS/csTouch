using System.ComponentModel.Composition;
using Caliburn.Micro;
using DataServer;
using TasksPlugin.Interfaces;

namespace TasksPlugin.ViewModels
{
    [Export(typeof(ITasks))]
    public class TasksViewModel : Screen
    {
        // TODO Add a filter, such that only tasks are shown which are of interest to you.

        private object selectedResponse;
        private Task selectedTask;
        public TasksViewModel() {
            Tasks = new BindableCollection<Task>();
            Responses = new BindableCollection<object>();
        }

        public BindableCollection<Task> Tasks { get; set; }

        public Task SelectedTask {
            get { return selectedTask; }
            set {
                if (selectedTask == value) return;
                selectedTask = value;
                NotifyOfPropertyChange(() => SelectedTask);
            }
        }

        public BindableCollection<object> Responses { get; set; }

        public object SelectedResponse {
            get { return selectedResponse; }
            set { selectedResponse = value; NotifyOfPropertyChange(() => SelectedResponse); }
        }


    }
}
