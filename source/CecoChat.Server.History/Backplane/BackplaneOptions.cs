using CecoChat.Kafka;

namespace CecoChat.Server.History.Backplane;

public sealed class BackplaneOptions
{
    public KafkaOptions Kafka { get; set; } = new();

    public KafkaConsumerOptions HistoryConsumer { get; set; } = new();

    public string TopicMessagesByReceiver { get; set; } = string.Empty;
}