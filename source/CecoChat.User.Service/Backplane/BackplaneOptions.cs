using Common.Kafka;

namespace CecoChat.User.Service.Backplane;

public sealed class BackplaneOptions
{
    public KafkaOptions Kafka { get; init; } = new();

    public KafkaProducerOptions ConnectionsProducer { get; init; } = new();

    public string TopicMessagesByReceiver { get; init; } = string.Empty;
}
