using CecoChat.Contracts.Backplane;
using Confluent.Kafka;
using Google.Protobuf;
using SerializationContext = Confluent.Kafka.SerializationContext;

namespace CecoChat.Server.Backplane
{
    public sealed class BackplaneMessageSerializer : ISerializer<BackplaneMessage>
    {
        public byte[] Serialize(BackplaneMessage data, SerializationContext context)
        {
            byte[] bytes = data.ToByteArray();
            return bytes;
        }
    }
}
