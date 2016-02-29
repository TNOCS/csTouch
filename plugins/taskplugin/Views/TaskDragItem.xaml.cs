using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using DataServer;
using Microsoft.Surface.Presentation;
using csShared;
using csShared.Utils;

namespace TasksPlugin.Views {
    /// <summary>
    ///     Interaction logic for TaskDragItem.xaml
    /// </summary>
    public partial class TaskDragItem {
        // Using a DependencyProperty as the backing store for Task.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TaskProperty =
            DependencyProperty.Register("Task", typeof (Task), typeof (TaskDragItem), new PropertyMetadata(null));
        
        // Using a DependencyProperty as the backing store for Plugin.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PluginProperty =
            DependencyProperty.Register("Plugin", typeof (TaskPlugin), typeof (TaskDragItem), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for Service.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ServiceProperty =
            DependencyProperty.Register("Service", typeof (PoiService), typeof (TaskDragItem), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for FloatingElementId.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FloatingElementIdProperty =
            DependencyProperty.Register("FloatingElementId", typeof(string), typeof(TaskDragItem), new PropertyMetadata(null));

        private readonly List<InputDevice> ignoredDeviceList = new List<InputDevice>();

        public TaskDragItem() {
            InitializeComponent();
            Loaded += TaskDragItem_Loaded;
        }

        public Task Task {
            get { return (Task) GetValue(TaskProperty); }
            set { SetValue(TaskProperty, value); }
        }

        public TaskPlugin Plugin {
            get { return (TaskPlugin) GetValue(PluginProperty); }
            set { SetValue(PluginProperty, value); }
        }

        public PoiService Service {
            get { return (PoiService)GetValue(ServiceProperty); }
            set { SetValue(ServiceProperty, value); }
        }

        public string FloatingElementId {
            get { return (string)GetValue(FloatingElementIdProperty); }
            set { SetValue(FloatingElementIdProperty, value); }
        }

        private static AppStateSettings AppState {
            get { return AppStateSettings.Instance; }
        }

        private bool IsDragging { get; set; }

        private void TaskDragItem_Loaded(object sender, RoutedEventArgs e) {
            TouchDown += TaskDragItem_TouchDown;
            MouseDown += TaskDragItem_TouchDown;
            AppState.Drop += AppState_Drop;
            
            if (!IsDragging) return;
            SurfaceDragDrop.AddDragCompletedHandler(this, DragCompleted);
            AppState.StoreVisibleState();
            AppState.BottomTabMenuVisible = AppState.LeftTabMenuVisible = AppState.DockedFloatingElementsVisible = AppState.TimelineVisible = false;
        }

        void TaskDragItem_TouchDown(object sender, InputEventArgs e) {
            if (IsDragging || Plugin.IsTaskActive) return;
            Plugin.ActiveTask = Task;
            StartDragDrop(sender, e.Device, this);
            e.Handled = true;
        }

        private void AppState_Drop(object sender, DropEventArgs e) {
            if (!(e.EventArgs.Cursor.Data is TaskDragItem)) return;
            AppState.RestoreVisibleState();
            Plugin.ActiveTask = null;
        }

        private void DragCompleted(object sender, SurfaceDragCompletedEventArgs e) {
            AppState.RestoreVisibleState();
        }

        public void StartDragDrop(object sender, InputDevice device, object src) {
            //if (!layer.Service.Settings.CanEdit) return;

            var mf = sender;

            // Check whether the input device is in the ignore list.
            if (ignoredDeviceList.Contains(device)) return;

            InputDeviceHelper.InitializeDeviceState(device);

            // try to start drag-and-drop,
            // verify that the cursor the contact was placed at is a ListBoxItem
            //                DependencyObject downSource = sender as DependencyObject;
            //                FrameworkElement parrent = mf.Parent as FrameworkElement;
            //                var source = GetVisualAncestor<FrameworkElement>(downSource);

            var findSource = src as FrameworkElement;

            //var parrent = GetVisualAncestor<FrameworkElement>(downSource);

            //                var cursorVisual = new ucDocument(){Document = new Document(){Location = mf.Location}};
            //                var cursorVisual = new ListFeatureView() { CanBeDragged = false };
            var cursorVisual = new TaskDragItem {
                Task = Task,
                IsDragging = true,
                Plugin = Plugin,
                DataContext = mf,
                Width = 75,
                Height = 75
            };


            var devices = new List<InputDevice>(new[] {device});

            SurfaceDragDrop.BeginDragDrop(this, findSource, cursorVisual, mf, devices, DragDropEffects.Copy);

            // Reset the input device's state.
            InputDeviceHelper.ClearDeviceState(device);
            ignoredDeviceList.Remove(device);
        }
    }
}