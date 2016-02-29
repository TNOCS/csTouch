#region

using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Media;
using Caliburn.Micro;
using csShared;

#endregion

namespace csCommon.Plugins.ServicesPlugin
{
    public interface IServices {}

    public class ServicesConfig : BindableCollection<ServiceInstance> {}

    [Export(typeof (IServices))]
    public class ServicesViewModel : Conductor<Screen>.Collection.OneActive
    {
        private static AppStateSettings AppState {
            get { return AppStateSettings.GetInstance(); }
        }

        public Brush AccentBrush {
            get { return AppState.AccentBrush; }
        }

        public ServicesPlugin Plugin { get; set; }


        public void Start(ServiceInstanceViewModel instance) {
            instance.Instance.Start();
        }

        public void Stop(ServiceInstanceViewModel instance) {
            instance.Instance.Stop();
        }

        protected override void OnViewLoaded(object view) {
            base.OnViewLoaded(view);

            if (Plugin.Services.Any()) ActiveItem = Plugin.Services.First();
        }

        public void StartAll() {
            Plugin.StartAllServices();
        }

        public void StopAll() {
            Plugin.StopAllServices();
        }
    }
}