using Caliburn.Micro;
using DataServer;
using csShared;
using csShared.Controls.Popups.MapCallOut;

namespace csDataServerPlugin {
    public class PingCallOutViewModel : Screen
    {
       
        private BaseContent ping;
       
        public AppStateSettings AppState {
            get { return AppStateSettings.Instance; }
        }

        private string pingText;

        public string PingText
        {
            get { return pingText; }
            set { pingText = value; NotifyOfPropertyChange(()=>PingText); }
        }
        

        public MapCallOutViewModel CallOut { get; set; }

        public BaseContent Ping
        {
            get { return ping; }
            set {
                ping = value;
                NotifyOfPropertyChange(() => Ping);
            }
        }

        protected override void OnViewLoaded(object view) {
            base.OnViewLoaded(view);
     
                PingText = (Ping.UserId != AppState.Imb.Status.Name) ? "Ping from " + Ping.UserId : "Ping";
        }
    }
}