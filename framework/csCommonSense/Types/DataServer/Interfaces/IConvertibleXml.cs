using System.Xml.Linq;
using DataServer;

namespace csCommon.Types.DataServer.Interfaces
{
    public interface IConvertibleXml
    {
        string XmlNodeId { get; }
        XElement ToXml();
        void FromXml(XElement element);
    }
}