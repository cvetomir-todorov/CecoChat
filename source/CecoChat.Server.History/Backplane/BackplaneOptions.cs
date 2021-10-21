using CecoChat.Kafka;

namespace CecoChat.Server.History.Backplane
{
    public sealed class BackplaneOptions
    {
        public KafkaOptions Kafka { get; set; }

        public KafkaConsumerOptions MaterializeConsumer { get; set; }

        public string MessagesTopicName { get; set; }
    }
}
