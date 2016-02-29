using System.Xml.Linq;
using DataServer;

namespace csCommon.Types.DataServer.Interfaces
{
    public interface IConvertibleXmlWithSettings : IConvertibleXml
    {
        XElement ToXml(ServiceSettings service);
    }
}