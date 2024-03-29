using CecoChat.Backplane.Contracts;
using Confluent.Kafka;
using Google.Protobuf;

namespace CecoChat.Backplane;

public sealed class BackplaneMessageSerializer : ISerializer<BackplaneMessage>
{
    public byte[] Serialize(BackplaneMessage data, SerializationContext context)
    {
        return data.ToByteArray();
    }
}

public sealed class BackplaneMessageDeserializer : IDeserializer<BackplaneMessage>
{
    public BackplaneMessage Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull)
        {
            return null!;
        }

        return BackplaneMessage.Parser.ParseFrom(data);
    }
}
