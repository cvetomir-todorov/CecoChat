using CecoChat.Kafka;
using CecoChat.Kafka.Health;

namespace CecoChat.Server.Messaging.Backplane;

public sealed class BackplaneOptions
{
    public KafkaOptions Kafka { get; init; } = new();

    public KafkaProducerOptions SendProducer { get; init; } = new();

    public KafkaConsumerOptions ReceiversConsumer { get; init; } = new();

    public string TopicMessagesByReceiver { get; init; } = string.Empty;

    public KafkaHealthOptions Health { get; init; } = new();
}
