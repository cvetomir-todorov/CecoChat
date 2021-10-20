using CecoChat.Kafka;

namespace CecoChat.Server.Materialize.Backplane
{
    public sealed class BackplaneOptions
    {
        public KafkaOptions Kafka { get; set; }

        public KafkaConsumerOptions MaterializeConsumer { get; set; }

        public string MessagesTopicName { get; set; }
    }
}
