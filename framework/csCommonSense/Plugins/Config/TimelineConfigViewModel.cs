using System.ComponentModel.Composition;
using Caliburn.Micro;
using csShared;
using csShared.Interfaces;


namespace csCommon.Plugins.Config
{

    

    [Export(typeof(IScreen))]
    public class TimelineConfigViewModel : Screen
    {

      
        public AppStateSettings AppState
        {
            get { return AppStateSettings.GetInstance(); }
            set { }
        }
        

        public ITimelineManager Timeline
        {
            get { return AppState.TimelineManager; }
        }


        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
          
        }

       
        


    }


}

