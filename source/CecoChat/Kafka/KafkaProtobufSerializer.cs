using CecoChat.ProtobufNet;
using Confluent.Kafka;
using SerializationContext = Confluent.Kafka.SerializationContext;

namespace CecoChat.Kafka
{
    public sealed class KafkaProtobufSerializer<TMessage> : ISerializer<TMessage>
    {
        private readonly ProtobufSerializer _serializer;

        public KafkaProtobufSerializer()
        {
            _serializer = new ProtobufSerializer();
        }

        public byte[] Serialize(TMessage data, SerializationContext context)
        {
            byte[] bytes = _serializer.SerializeToByteArray(data);
            return bytes;
        }
    }
}
