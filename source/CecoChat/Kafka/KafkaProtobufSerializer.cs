using CecoChat.ProtobufNet;
using Confluent.Kafka;
using SerializationContext = Confluent.Kafka.SerializationContext;

namespace CecoChat.Kafka
{
    public sealed class KafkaProtobufSerializer<TMessage> : ISerializer<TMessage>
    {
        private readonly GenericSerializer<TMessage> _serializer;

        public KafkaProtobufSerializer()
        {
            _serializer = new GenericSerializer<TMessage>();
        }

        public byte[] Serialize(TMessage data, SerializationContext context)
        {
            byte[] bytes = _serializer.SerializeToByteArray(data);
            return bytes;
        }
    }
}
