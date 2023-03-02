using CecoChat.Kafka;
using CecoChat.Kafka.Health;

namespace CecoChat.Server.State.Backplane;

public sealed class BackplaneOptions
{
    public KafkaOptions Kafka { get; set; } = new();

    public KafkaConsumerOptions ReceiversConsumer { get; set; } = new();

    public KafkaConsumerOptions SendersConsumer { get; set; } = new();

    public string TopicMessagesByReceiver { get; set; } = string.Empty;

    public KafkaHealthOptions Health { get; set; } = new();
}
