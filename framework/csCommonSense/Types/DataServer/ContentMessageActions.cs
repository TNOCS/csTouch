using ProtoBuf;

namespace DataServer
{
    [ProtoContract]
    public enum ContentMessageActions
    {
        Add,
        Remove,
        Update,
        Ping
    }
}