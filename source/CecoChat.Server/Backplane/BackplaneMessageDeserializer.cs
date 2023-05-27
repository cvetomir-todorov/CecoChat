using CecoChat.Contracts.Backplane;
using Confluent.Kafka;
using Google.Protobuf;
using SerializationContext = Confluent.Kafka.SerializationContext;

namespace CecoChat.Server.Backplane;

public sealed class BackplaneMessageDeserializer : IDeserializer<BackplaneMessage>
{
    public BackplaneMessage Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull)
        {
            return null!;
        }

        BackplaneMessage message = new();
        message.MergeFrom(data.ToArray());
        return message;
    }
}
