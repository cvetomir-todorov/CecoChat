using System;
using Confluent.Kafka;
using ProtoBuf;
using SerializationContext = Confluent.Kafka.SerializationContext;

namespace CecoChat.Kafka
{
    public sealed class KafkaProtobufDeserializer<TMessage> : IDeserializer<TMessage>
    {
        public TMessage Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        {
            if (isNull)
                return default;

            TMessage message = Serializer.Deserialize<TMessage>(data);
            return message;
        }
    }
}
