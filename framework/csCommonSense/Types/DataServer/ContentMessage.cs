using System;
using System.IO;
using ProtoBuf;

namespace DataServer
{
    [ProtoContract]
    public class ContentMessage
    {
        [ProtoMember(1)]
        public int Sender { get; set; }
        [ProtoMember(2)]
        public ContentMessageActions Action { get; set; }
        [ProtoMember(3)]
        public byte[] Content { get; set; }
        [ProtoMember(4)]
        public Guid Id { get; set; }
        [ProtoMember(5)]
        public string ContentList { get; set; }
        [ProtoMember(6)]
        public Type ContentType { get; set; }

        private const int MessageSeparator = 71;

        public static ContentMessage ConvertByteArrayToMessage(byte[] buffer)
        {
            using (var ms = new MemoryStream())
            {
                ms.Write(buffer, 0, buffer.Length);
                ms.Position = 0;
                return Serializer.DeserializeWithLengthPrefix<ContentMessage>(ms, PrefixStyle.Base128, MessageSeparator);
                //message = (Message)model.Deserialize(ms, null, typeof(Message));
            }
        }

        public MemoryStream ConvertToStream()
        {
            var ms = new MemoryStream();
            
            Serializer.SerializeWithLengthPrefix(ms, this, PrefixStyle.Base128, MessageSeparator);
            return ms;
        }
    }
}