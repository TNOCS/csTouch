using System.Collections.Generic;

namespace csShared.FloatingElements.Classes
{

    public class EndPoint
    {
        public string Title { get; set; }
        public string Id { get; set; }
        public string ContractType { get; set; }
        public IFloatingShareContract Contract { get; set; }
        public Dictionary<string, object> Labels { get; set; } 

        public object Value { get; set; }
    }
    public interface IFloatingShareContract
    {
        string Name { get; }
        List<string> Contracts { get; }
        List<EndPoint> GetEndPoints(Dictionary<string, object> contracts); 
        void Send(EndPoint endPoint, FloatingContainer fc);
    }
}
