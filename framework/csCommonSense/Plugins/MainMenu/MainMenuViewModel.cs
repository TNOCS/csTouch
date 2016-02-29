using System.Collections.ObjectModel;
using Caliburn.Micro;
using csCommon.MapPlugins.StartTabPanel;
using csShared;
using csShared.Geo;
using System.ComponentModel.Composition;

namespace csCommon.MapPlugins.MainMenu
{

    public interface IMainMenuSelection
    {}

    [Export(typeof(IMainMenuSelection))]
    public class MainMenuViewModel : Screen, IMainMenuSelection
    {

        
        public AppStateSettings AppState { get { return AppStateSettings.Instance; } set { }
        }
        public FloatingElement Element { get; set; }

        private ObservableCollection<MenuItem> _menuItems = AppStateSettings.Instance.MainMenuItems;

        public ObservableCollection<MenuItem> MenuItems
        {
            get { return _menuItems; }
            set { _menuItems = value; NotifyOfPropertyChange(()=>MenuItems); }
        }

        private StartTabPanelViewModel _tabPanel;

        public StartTabPanelViewModel TabPanel
        {
            get { return _tabPanel; }
            set { _tabPanel = value; }
        }




        public MapViewDef ViewDef
        {
            get
            {
                return AppState.ViewDef;
            }
        }

        protected override void OnActivate()
        {
            base.OnActivate();
        
        }

        private MainMenuView _mtv;

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            _mtv = (MainMenuView) view;

           
        }

        public void Click(MenuItem mi,object b)
        {
            mi.ForceClick();
        }
        

        public MainMenuViewModel()
        {
            // Default caption
            Caption = "Main Menu";

        }


        

        public string Caption { get; set; }


    }
}
