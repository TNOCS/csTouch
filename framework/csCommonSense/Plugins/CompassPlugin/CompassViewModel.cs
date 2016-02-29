using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Media;
using Caliburn.Micro;
using csCommon.Plugins.Config;
using csShared;

namespace csCommon.Plugins.CompassPlugin
{

  

    [Export(typeof(IScreen))]
    public class CompassViewModel : Screen
    {

        
        public AppStateSettings AppState
        {
            get { return AppStateSettings.GetInstance(); }
            set { }
        }


        public FloatingElement Element { get; set; }
        

        public Brush AccentBrush
        {
            get { return AppState.AccentBrush; }
        }

       
        

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
          
            
        }

        


    }


}

