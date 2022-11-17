using CecoChat.Kafka;

namespace CecoChat.Server.Messaging.Backplane;

public sealed class BackplaneOptions
{
    public KafkaOptions Kafka { get; set; } = new();

    public KafkaProducerOptions SendProducer { get; set; } = new();

    public KafkaConsumerOptions ReceiversConsumer { get; set; } = new();

    public string TopicMessagesByReceiver { get; set; } = string.Empty;
}
