using Common.Kafka;

namespace CecoChat.Config.Backplane;

internal class BackplaneOptions
{
    public KafkaOptions Kafka { get; init; } = new();

    public KafkaProducerOptions ConfigChangesProducer { get; init; } = new();

    public KafkaConsumerOptions ConfigChangesConsumer { get; init; } = new();

    public string TopicConfigChanges { get; init; } = string.Empty;
}
