using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows;
using Caliburn.Micro;
using csShared;
using csShared.Geo;
using csShared.Interfaces;
using csShared.Timeline;

namespace nl.tno.cs.presenter
{
    [Export(typeof (IModule))]
    public class PresenterViewModel : Screen, IModule
    {
        private PresenterView _view;
        //private AttractDemo ad;
        private IMetroExplorer _metroExplorer;

        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
            set { }
        }

        public ITimelineManager TimelineManager { get; set; }

        public IMetroExplorer MetroExplorer { get
            {
                return _metroExplorer;
            }}
        

        #region IModule Members

        public string Name
        {
            get { return "Presenter"; }
        }

        


        public void InitializeApp()
        {
            _metroExplorer = new MetroExplorerViewModel(Directory.GetCurrentDirectory() + @"\TNO");
        }

        public void StartApp()
        {
            
           

        }

        #endregion

        public void Switch()
        {
            AppState.DockedFloatingElementsVisible = !AppState.DockedFloatingElementsVisible;
        }

        protected override void OnViewLoaded(object view)
        {            
            base.OnViewLoaded(view);
            _view = (PresenterView)view;
           
        }
    }
}