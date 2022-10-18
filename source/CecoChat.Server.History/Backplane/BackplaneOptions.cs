using CecoChat.Kafka;

namespace CecoChat.Server.History.Backplane;

public sealed class BackplaneOptions
{
    public KafkaOptions Kafka { get; set; }

    public KafkaConsumerOptions HistoryConsumer { get; set; }

    public string TopicMessagesByReceiver { get; set; }
}