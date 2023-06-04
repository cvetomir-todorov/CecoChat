using CecoChat.Kafka;
using CecoChat.Kafka.Health;

namespace CecoChat.Server.History.Backplane;

public sealed class BackplaneOptions
{
    public KafkaOptions Kafka { get; init; } = new();

    public KafkaConsumerOptions HistoryConsumer { get; init; } = new();

    public string TopicMessagesByReceiver { get; init; } = string.Empty;

    public KafkaHealthOptions Health { get; init; } = new();
}
