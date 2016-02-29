#region

using System;
using System.Collections.Generic;

#endregion

namespace csShared.FloatingElements.Classes
{
    public class QrShareContract : IFloatingShareContract
    {
        public AppStateSettings AppState {
            get { return AppStateSettings.Instance; }
        }

        public string Name {
            get { return "Qr"; }
        }

        public List<string> Contracts {
            get { return new List<string> {"link"}; }
        }

        public List<EndPoint> GetEndPoints(Dictionary<string, object> contracts)
        {
            var ep = new List<EndPoint>();
            foreach (var c in contracts)
            {
                if (c.Key == "link")
                {
                    Uri uriResult;
                    if (c.Key == "link" && Uri.TryCreate(c.Value.ToString(), UriKind.Absolute, out uriResult))
                    {
                        if (uriResult.Scheme == Uri.UriSchemeHttp)
                        {

                            ep.Add(new EndPoint { Title = "QR", Contract = this, ContractType = "link" });
                        }
                    }
                }
            }
            
            return ep;
        }

        public void Send(EndPoint endPoint, FloatingContainer fc) {

            
            fc._fe.ModelInstanceBack = new QrViewModel { EndPoint = endPoint, Element = fc._fe };
            fc._fe.CanFlip = true;
            fc._fe.Flip();


//            AppState.FloatingItems.AddFloatingElement(fe);
        }
    }
}