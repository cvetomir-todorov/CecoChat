using CecoChat.Kafka;
using CecoChat.Kafka.Health;

namespace CecoChat.Server.User.Backplane;

public sealed class BackplaneOptions
{
    public KafkaOptions Kafka { get; init; } = new();

    public KafkaProducerOptions ConnectionsProducer { get; init; } = new();

    public string TopicMessagesByReceiver { get; init; } = string.Empty;

    public KafkaHealthOptions Health { get; init; } = new();
}
