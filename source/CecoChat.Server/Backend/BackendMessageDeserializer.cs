using System;
using CecoChat.Contracts.Backplane;
using Confluent.Kafka;
using Google.Protobuf;
using SerializationContext = Confluent.Kafka.SerializationContext;

namespace CecoChat.Server.Backend
{
    public sealed class BackendMessageDeserializer : IDeserializer<BackplaneMessage>
    {
        public BackplaneMessage Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        {
            if (isNull)
            {
                return default;
            }

            BackplaneMessage message = new();
            message.MergeFrom(data.ToArray());
            return message;
        }
    }
}
