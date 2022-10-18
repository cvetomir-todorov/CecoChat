using CecoChat.Kafka;

namespace CecoChat.Server.Messaging.Backplane;

public sealed class BackplaneOptions
{
    public KafkaOptions Kafka { get; set; }

    public KafkaProducerOptions SendProducer { get; set; }

    public KafkaConsumerOptions ReceiversConsumer { get; set; }

    public KafkaConsumerOptions ReplicatingConsumer { get; set; }

    public KafkaProducerOptions ReplicatingProducer { get; set; }

    public string TopicMessagesByReceiver { get; set; }

    public string TopicMessagesBySender { get; set; }
}