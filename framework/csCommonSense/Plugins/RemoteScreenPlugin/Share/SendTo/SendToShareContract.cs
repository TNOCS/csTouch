#region

using System.Collections.Generic;
using System.Linq;
using csImb;
using csShared;
using csShared.FloatingElements;
using csShared.FloatingElements.Classes;

#endregion
 
namespace csRemoteScreenPlugin.Share.SendTo
{
    public class SendToContract : IFloatingShareContract
    {
        public AppStateSettings AppState {
            get { return AppStateSettings.Instance; }
        }

        public string Name {
            get { return "SendTo"; }
        }

        public List<string> Contracts {
            get { return new List<string> {"link"}; }
        }

        public List<EndPoint> GetEndPoints(Dictionary<string,object> contracts) {
            var ep = new List<EndPoint>();
            if (AppState.Imb.ActiveGroup != null)
            {
                var e = new EndPoint { Title = "Send To group", Contract = this, ContractType = "link" };
                e.Labels = new Dictionary<string, object>();
                e.Labels["group"] = AppState.Imb.ActiveGroup;
                
                ep.Add(e);
            }
            else
            {
                foreach (ImbClientStatus a in AppStateSettings.Instance.Imb.ScreenshotReceivingClients.Where(k => k.Client))
                {
                    var e = new EndPoint { Title = "Send To " + a.Name, Contract = this, ContractType = "link" };
                    e.Labels = new Dictionary<string, object>();
                    e.Labels["client"] = a;
                    ep.Add(e);

                }
            }
            
            return ep;
        }

        public void Send(EndPoint endPoint, FloatingContainer fc) {

            if (endPoint.Labels != null && endPoint.Labels.ContainsKey("group"))
            {
                var cs = (csGroup)endPoint.Labels["group"];
                if (cs != null)
                {
                    foreach (var m in cs.FullClients.Where(k => k.Capabilities.Contains("receivescreenshot")))
                    {
                        AppState.Imb.SendElementImage(m, fc._cpView);
                    }
                }

            }
            if (endPoint.Labels!=null && endPoint.Labels.ContainsKey("client"))
            {
                var cs = (ImbClientStatus) endPoint.Labels["client"];
                if (cs != null)
                {
                    AppState.Imb.SendElementImage(cs, fc._cpView);
                }

            }
           


//            AppState.FloatingItems.AddFloatingElement(fe);
        }
    }
}