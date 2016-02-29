#region

using System;
using System.Collections.Generic;

#endregion

namespace csShared.FloatingElements.Classes
{
    public class EmailShareContract : IFloatingShareContract
    {
        public AppStateSettings AppState {
            get { return AppStateSettings.Instance; }
        }

        public string Name {
            get { return "Email"; }
        }

        public List<string> Contracts {
            get { return new List<string> {"link", "document"}; }
        }

        public List<EndPoint> GetEndPoints(Dictionary<string,object> contracts) {
            var ep = new List<EndPoint>();
            if (AppState.Config.Get(@"EmailShare.ToAddress", "") == "") return ep;
            foreach (var c in contracts)
            {
                Uri uriResult;
                if (c.Key == "link" && Uri.TryCreate(c.Value.ToString(), UriKind.Absolute, out uriResult))
                {
                    if (uriResult.Scheme == Uri.UriSchemeHttp)
                    {
                        ep.Add(new EndPoint { Title = "Mail Link", Contract = this, ContractType = "link" });
                    }
                }
                if (c.Key == "document")
                {

                    ep.Add(new EndPoint { Title = "Mail File", Contract = this, ContractType = "document" });
                }

            }
            
            return ep;
        }

        public void Send(EndPoint endPoint, FloatingContainer fc) {
            fc._fe.ModelInstanceBack = new SendMailViewModel { EndPoint = endPoint, Element = fc._fe };
            fc._fe.CanFlip = true;
            fc._fe.Flip();
        }
    }
}