#region

using System.ComponentModel.Composition;
using System.Windows.Threading;
using Caliburn.Micro;
using csCommon.csMapCustomControls.CircularMenu;
using csShared;

#endregion

namespace csCommon.Plugins.CircularMenu
{
    public interface ICircularMenuViewModel
    {
    }

    [Export(typeof (ICircularMenuViewModel))]
    public class CircularMenuViewModel : Screen, ICircularMenuViewModel
    {
        private CircularMenuView cv;
        private CircularMenuItem rootMenuItem;

        public CircularMenuViewModel()
        {
        }

        public CircularMenuViewModel(DispatcherTimer updateMapTimer)
        {
            // Default caption
            Caption = "MapTools";
        }

        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
            set { }
        }

        public FloatingElement Element { get; set; }

        public CircularMenuItem RootMenuItem
        {
            get { return rootMenuItem; }
            set
            {
                rootMenuItem = value;
                NotifyOfPropertyChange(() => RootMenuItem);
            }
        }

        public string Caption { get; set; }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            cv = (CircularMenuView) view;

            cv.cmMain.Content = rootMenuItem;
            cv.cmMain.RootItem = rootMenuItem;
            cv.cmMain.SelectedItem = rootMenuItem;
            cv.cmMain.Draw();
            if (rootMenuItem != null)
            {
                rootMenuItem.PropertyChanged += cv.cmMain.RootItem_PropertyChanged;
            }
        }
    }
}