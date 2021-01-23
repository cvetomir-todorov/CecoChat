using System;
using CecoChat.ProtobufNet;
using Confluent.Kafka;
using SerializationContext = Confluent.Kafka.SerializationContext;

namespace CecoChat.Kafka
{
    public sealed class KafkaProtobufDeserializer<TMessage> : IDeserializer<TMessage>
    {
        private readonly GenericSerializer<TMessage> _serializer;

        public KafkaProtobufDeserializer()
        {
            _serializer = new GenericSerializer<TMessage>();
        }

        public TMessage Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        {
            if (isNull)
                return default;

            TMessage message = _serializer.DeserializeFromSpan(data);
            return message;
        }
    }
}
