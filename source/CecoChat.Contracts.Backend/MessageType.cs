using ProtoBuf;

namespace CecoChat.Contracts.Backend
{
    [ProtoContract]
    public enum MessageType
    {
        Unknown = 0,
        PlainText = 1,
        Ack = 2
    }
}