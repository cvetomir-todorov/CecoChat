using ProtoBuf;

namespace CecoChat.Contracts.Backend
{
    [ProtoContract]
    [ProtoInclude(5, typeof(PlainTextMessage))]
    public abstract class Message
    {
        [ProtoMember(1)]
        public int MessageID { get; set; }

        [ProtoMember(2)]
        public int SenderID { get; set; }

        [ProtoMember(3)]
        public int ReceiverID { get; set; }

        [ProtoMember(4)]
        public MessageType Type { get; set; }

        public override string ToString()
        {
            return $"[{MessageID}] {SenderID}->{ReceiverID} {Type}";
        }
    }
}