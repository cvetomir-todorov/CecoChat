using CecoChat.Kafka;

namespace CecoChat.Server.State.Backplane
{
    public sealed class BackplaneOptions
    {
        public KafkaOptions Kafka { get; set; }

        public KafkaConsumerOptions StateConsumer { get; set; }

        public string MessagesTopicName { get; set; }
    }
}