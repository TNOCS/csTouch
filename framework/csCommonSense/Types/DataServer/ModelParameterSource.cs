using ProtoBuf;

namespace DataServer
{
    [ProtoContract]
    public enum ModelParameterSource
    {
        direct,
        label
    }
}