using CecoChat.Config.Contracts;
using Confluent.Kafka;
using Google.Protobuf;

namespace CecoChat.DynamicConfig.Backplane;

public sealed class ConfigChangeSerializer : ISerializer<ConfigChange>
{
    public byte[] Serialize(ConfigChange data, SerializationContext context)
    {
        return data.ToByteArray();
    }
}

public sealed class ConfigChangeDeserializer : IDeserializer<ConfigChange>
{
    public ConfigChange Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull)
        {
            return null!;
        }

        return ConfigChange.Parser.ParseFrom(data);
    }
}
