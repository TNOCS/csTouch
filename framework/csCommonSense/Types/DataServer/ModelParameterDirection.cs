using ProtoBuf;

namespace DataServer
{
    [ProtoContract]
    public enum ModelParameterDirection
    {
        input,
        output
    }
}