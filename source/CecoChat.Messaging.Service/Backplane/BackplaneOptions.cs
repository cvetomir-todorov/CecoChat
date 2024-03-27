using Common.Kafka;

namespace CecoChat.Messaging.Service.Backplane;

public sealed class BackplaneOptions
{
    public KafkaOptions Kafka { get; init; } = new();

    public KafkaProducerOptions SendProducer { get; init; } = new();

    public KafkaConsumerOptions ReceiversConsumer { get; init; } = new();

    public string TopicMessagesByReceiver { get; init; } = string.Empty;
}
