using ProtoBuf;

namespace DataServer
{
    [ProtoContract]
    public enum PrivateMessageActions
    {
        Subscribe,
        SubscribeRequest,
        Unsubscribe,
        UnsubscribeRequest,
        ServiceReset,
        ListReset,
        RequestData,
        ResponseData,
        SendData,
        RequestXml,
        ResponseXml        
    }
}