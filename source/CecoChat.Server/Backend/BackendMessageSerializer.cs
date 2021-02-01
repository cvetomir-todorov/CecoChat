using CecoChat.Contracts.Backend;
using Confluent.Kafka;
using Google.Protobuf;
using SerializationContext = Confluent.Kafka.SerializationContext;

namespace CecoChat.Server.Backend
{
    public sealed class BackendMessageSerializer : ISerializer<BackendMessage>
    {
        public byte[] Serialize(BackendMessage data, SerializationContext context)
        {
            byte[] bytes = data.ToByteArray();
            return bytes;
        }
    }
}
