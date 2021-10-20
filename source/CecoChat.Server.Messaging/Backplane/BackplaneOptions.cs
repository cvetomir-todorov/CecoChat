using CecoChat.Kafka;

namespace CecoChat.Messaging.Server.Backplane
{
    public sealed class BackplaneOptions
    {
        public KafkaOptions Kafka { get; set; }

        public KafkaProducerOptions SendProducer { get; set; }

        public KafkaConsumerOptions ReceiversConsumer { get; set; }

        public string ServerID { get; set; }

        public string MessagesTopicName { get; set; }
    }
}
