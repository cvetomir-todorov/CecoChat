using CecoChat.Kafka;
using CecoChat.Kafka.Health;

namespace CecoChat.Server.Admin.Backplane;

public sealed class BackplaneOptions
{
    public KafkaOptions Kafka { get; init; } = new();

    public KafkaProducerOptions ConfigChangesProducer { get; init; } = new();

    public string ConfigChangesTopic { get; init; } = string.Empty;

    public KafkaHealthOptions Health { get; init; } = new();
}
