using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Threading;
using Caliburn.Micro;
using csCommon.Plugins.AppStatus;
using csShared;
using csShared.Utils;
using IMB3;

namespace csCommon.Plugins.Config
{

    

    [Export(typeof(IScreen))]
    public class BasicViewModel : Screen
    {

        //private string port;

        //private string server;
        //private string name;
        
        public AppStateSettings AppState
        {
            get { return AppStateSettings.GetInstance(); }
            set { }
        }


        private BindableCollection<ImbConnectionString> connections = new BindableCollection<ImbConnectionString>();


       

        private readonly BackgroundWorker bw = new BackgroundWorker();

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            
           
        }

        


    }


}

