using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using Caliburn.Micro;
using csCommon.csMapCustomControls.CircularMenu;
using csShared;
using csShared.FloatingElements;
using csShared.Interfaces;
using csShared.TabItems;

namespace csCommon.Plugins.CircularMenu
{
    [Export(typeof(IPlugin))]
    public class CircularMenuPlugin : PropertyChangedBase, IPlugin
    {

        private IPluginScreen _screen;

        public IPluginScreen Screen
        {
            get { return _screen; }
            set { _screen = value; NotifyOfPropertyChange(() => Screen); }
        }

        public bool CanStop { get { return true; } }

        private ISettingsScreen _settings;

        public ISettingsScreen Settings
        {
            get { return _settings; }
            set { _settings = value; NotifyOfPropertyChange(() => Settings); }
        }

        private bool _hideFromSettings = true;

        public bool HideFromSettings
        {
            get { return _hideFromSettings; }
            set { _hideFromSettings = value; NotifyOfPropertyChange(()=>HideFromSettings); }
        }
        

        public int Priority
        {
            get { return 3; }
        }

        public string Icon
        {
            get { return @"icons\bookmarks.png"; }           
        }
        

        private string _file;

        public string BFile
        {
            get { return _file; }
            set { _file = value; NotifyOfPropertyChange(()=>BFile); }
        }
        


        #region IPlugin Members

        public string Name
        {
            get { return "CircularMenuPlugin"; }
        }

        private bool _isRunning;

        public bool IsRunning
        {
            get { return _isRunning; }
            set { _isRunning = value; NotifyOfPropertyChange(() => IsRunning); }
        }

        public AppStateSettings AppState { get; set; }

        public FloatingElement Element { get; set; }
        private StartPanelTabItem tabItem { get; set; }

        private List<Point> spots = new List<Point>();

        public void Init()
        {
            AppState.CircularMenus.CollectionChanged += CircularMenus_CollectionChanged;
            for (int i = 1;i<10;i++)
            {
                spots.Add(new Point(i * 85.0 + 300, 27.5));
            }
          
        }

        Point FindNextSpot()
        {
            return new Point(AppState.CircularMenus.Count(k => !k.StartPosition.HasValue) * 85.0 + 300, 37.5);
            // FIXME TODO: Unreachable code
//            var sp = AppState.FloatingItems.Where(k => k.StartPosition.HasValue);
//            foreach (var s in spots)
//            {
//                
//                if (!sp.Any(k => k.StartPosition.Value == s))
//                {
//                    return s;
//                }
//            }
//            return new Point(200,200);
        }

        void CircularMenus_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (CircularMenuItem mi in e.OldItems)
                {
                    var old =
                        AppState.FloatingItems.FirstOrDefault(
                            k =>
                            k.ModelInstance is CircularMenuViewModel &&
                            ((CircularMenuViewModel) k.ModelInstance).RootMenuItem.Id == mi.Id);

                    if (old != null)
                    {
                        AppState.FloatingItems.RemoveFloatingElement(old);
                    }
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (CircularMenuItem mi in e.NewItems) {

                    CircularMenuViewModel viewModel = new CircularMenuViewModel() {RootMenuItem = mi};
                    if (string.IsNullOrEmpty(mi.Id)) mi.Id = Guid.NewGuid().ToString();
                    Point start = FindNextSpot();
                    if (mi.StartPosition.HasValue) {
                        start = mi.StartPosition.Value;
                    }

                    // TODO EV Check, used to be var Element
                    Element = FloatingHelpers.CreateFloatingElement("Menu", start, new Size(400, 400), viewModel);
                    var res = Application.Current.FindResource("SimpleContainer");
                    Element.Style = res as Style;

                    if (mi.AutoCloseTimeout > 0) {
                        Element.AutoCloseTimeout = mi.AutoCloseTimeout;
                        Element.AutoClose = FloatingElement.AutoCloseStyle.NoInteraction;
                    }
                    viewModel.Element = Element;
                    Element.CanRotate = false;
                    Element.CanScale = false;
                    Element.RemoveOnEdge = false;
                    Element.StartPosition = start;
                    Element.ResetOnEdge = true;
                    AppState.FloatingItems.AddFloatingElement(Element);
                }
            }
        }
      

        public void Start()
        {
            IsRunning = true;
        }

        void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            
        }

        public void Pause()
        {
            IsRunning = false;
          
        }

        public void Stop()
        {
            IsRunning = false;
        }

        

       

        #endregion


       
    }
}
