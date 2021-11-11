using CecoChat.Kafka;

namespace CecoChat.Server.Messaging.Backplane
{
    public sealed class BackplaneOptions
    {
        public KafkaOptions Kafka { get; set; }

        public KafkaProducerOptions SendProducer { get; set; }

        public KafkaConsumerOptions ReceiversConsumer { get; set; }

        public string ServerID { get; set; }

        public string TopicMessagesByReceiver { get; set; }
    }
}
