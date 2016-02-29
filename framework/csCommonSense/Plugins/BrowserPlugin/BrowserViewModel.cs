using System;
using System.Collections.ObjectModel;
using Caliburn.Micro;
using csCommon.MapPlugins.StartTabPanel;
using csGeoLayers;
using csShared;
using csShared.Geo;
using System.ComponentModel.Composition;

namespace csCommon.Plugins.BrowserPlugin
{


    [Export(typeof(IScreen))]
    public class BrowserViewModel : Screen
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
                return AppState.ViewDef; }            
        }

        protected override void OnActivate()
        {
            base.OnActivate();
        
        }

        private BrowserView bv;

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            bv = (BrowserView) view;
            bv.wbBrowser.SuppressScriptErrors(true);
            AppState.ScriptCommand += AppState_ScriptCommand;

        }

        void AppState_ScriptCommand(object sender, string command)
        {
            if (command.StartsWith("web:"))
            {
                var url = command.Split(':')[1];
                if (bv != null)
                {
                    bv.wbBrowser.Source = new Uri(url);
                }
            }
        }

        public void Click(MenuItem mi,object b)
        {
            mi.ForceClick();
        }
        

        public BrowserViewModel()
        {
            // Default caption
            Caption = "Main Menu";

        }


        

        public string Caption { get; set; }


    }
}
