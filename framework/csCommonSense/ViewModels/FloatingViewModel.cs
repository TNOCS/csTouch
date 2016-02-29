using System.Windows;
using Caliburn.Micro;
using csShared;

namespace csCommon
{
    using System.ComponentModel.Composition;

    [Export(typeof(IFloating))]
    public class FloatingViewModel : Screen, IFloating
    {
        public AppStateSettings AppStateSettings { get { return AppStateSettings.Instance; } }

        public FloatingCollection FloatingItems { get { return AppStateSettings.FloatingItems;  } }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            Application.Current.MainWindow.SizeChanged += MainWindow_SizeChanged;
        }

        void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FloatingItems.OrderDockingFloatingElement();
        }
    }
}
