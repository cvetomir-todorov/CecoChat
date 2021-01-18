using System;
using ProtoBuf;

namespace CecoChat.Contracts.Backend
{
    [ProtoContract]
    [ProtoInclude(6, typeof(PlainTextMessage))]
    public abstract class Message
    {
        [ProtoMember(1)]
        public int MessageID { get; set; }

        [ProtoMember(2)]
        public int SenderID { get; set; }

        [ProtoMember(3)]
        public int ReceiverID { get; set; }

        [ProtoMember(4)]
        public DateTime Timestamp { get; set; }

        [ProtoMember(5)]
        public MessageType Type { get; set; }

        public override string ToString()
        {
            return $"[{MessageID} {Timestamp:F}] {SenderID}->{ReceiverID} {Type}";
        }
    }
}