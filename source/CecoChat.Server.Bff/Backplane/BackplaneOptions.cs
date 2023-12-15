using CecoChat.Kafka;
using CecoChat.Kafka.Health;

namespace CecoChat.Server.Bff.Backplane;

public sealed class BackplaneOptions
{
    public KafkaOptions Kafka { get; init; } = new();

    public KafkaHealthOptions Health { get; init; } = new();
}
