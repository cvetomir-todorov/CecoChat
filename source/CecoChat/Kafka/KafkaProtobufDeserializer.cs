using System;
using CecoChat.ProtobufNet;
using Confluent.Kafka;
using SerializationContext = Confluent.Kafka.SerializationContext;

namespace CecoChat.Kafka
{
    public sealed class KafkaProtobufDeserializer<TMessage> : IDeserializer<TMessage>
    {
        private readonly ProtobufSerializer _serializer;

        public KafkaProtobufDeserializer()
        {
            _serializer = new ProtobufSerializer();
        }

        public TMessage Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        {
            if (isNull)
                return default;

            TMessage message = _serializer.DeserializeFromSpan<TMessage>(data);
            return message;
        }
    }
}
