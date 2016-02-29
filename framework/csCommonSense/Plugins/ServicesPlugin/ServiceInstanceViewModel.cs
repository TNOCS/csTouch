using System.ComponentModel.Composition;
using System.Windows.Media;
using Caliburn.Micro;
using csShared;

namespace csCommon.Plugins.ServicesPlugin
{
    [Export(typeof(IScreen))]
    public class ServiceInstanceViewModel : Screen
    {
        public AppStateSettings AppState
        {
            get { return AppStateSettings.GetInstance(); }
            set { }
        }

        private ServiceInstance instance;

        public ServiceInstance Instance
        {
            get { return instance; }
            set { instance = value; NotifyOfPropertyChange(()=>Instance); }
        }

        public Brush AccentBrush
        {
            get { return AppState.AccentBrush; }
        }

        public void Stop()
        {
            Instance.Stop();
        }

        public void Start()
        {
            Instance.Start();
        }

        public void Clear()
        {
            Instance.Output.Clear();
        }
    }
}

