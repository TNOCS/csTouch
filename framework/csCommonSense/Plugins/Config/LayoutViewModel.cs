using System.ComponentModel.Composition;
using Caliburn.Micro;
using csShared;

namespace csCommon.Plugins.Config
{
    [Export(typeof(IScreen))]
    public class LayoutViewModel : Screen
    {
        //private string port;
        //private string server;
        //private string name;
        
        public AppStateSettings AppState
        {
            get { return AppStateSettings.GetInstance(); }
        }
        
        //private BindableCollection<ImbConnectionString> connections = new BindableCollection<ImbConnectionString>();

    }
}

