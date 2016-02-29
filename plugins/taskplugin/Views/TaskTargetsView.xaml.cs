using Microsoft.Surface.Presentation;
using TasksPlugin.ViewModels;

namespace TasksPlugin.Views
{
    /// <summary>
    /// Interaction logic for TaskTargetsView.xaml
    /// </summary>
    public partial class TaskTargetsView
    {
        public TaskTargetsView()
        {
            InitializeComponent();
        }

        private TaskTargetsViewModel ViewModel { get { return DataContext as TaskTargetsViewModel; } }

        private void SurfaceDragDrop_OnDragEnter(object sender, SurfaceDragDropEventArgs e) {
            if (ViewModel == null) return;
            ViewModel.DragEnter(sender, e);
        }

        private void SurfaceDragDrop_OnDrop(object sender, SurfaceDragDropEventArgs e) {
            if (ViewModel == null) return;
            ViewModel.Drop(sender, e);
        }

        private void Background_OnDrop(object sender, SurfaceDragDropEventArgs e) {
            if (ViewModel == null) return;
            ViewModel.DragDropCancelled(sender, e);          
        }
    }
}
