using CecoChat.Kafka;

namespace CecoChat.Server.User.Backplane;

public sealed class BackplaneOptions
{
    public KafkaOptions Kafka { get; init; } = new();

    public KafkaProducerOptions ConnectionsProducer { get; init; } = new();

    public string TopicMessagesByReceiver { get; init; } = string.Empty;
}
