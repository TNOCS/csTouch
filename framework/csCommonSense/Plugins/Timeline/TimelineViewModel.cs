using Caliburn.Micro;
using csShared;
using csShared.Interfaces;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace csCommon.Plugins.Timeline
{
    using System.ComponentModel.Composition;

    [Export(typeof(IPluginScreen))]
    public class TimelineViewModel : Screen, IPluginScreen
    {
        public static AppStateSettings AppState { get { return AppStateSettings.Instance; } }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            TimeLineView = (TimelineView)view;
            TimeLineView.SetBinding(FrameworkElement.VisibilityProperty,
                new Binding("TimelineVisible") { Source = AppState, Converter = new BooleanToVisibilityConverter() });
        }

        public Brush AccentBrush
        {
            get { return AppState.AccentBrush; }
        }

        public TimelineView TimeLineView { get; private set; }

        public string Name
        {
            get { return "Notification"; }
        }
    }
}
