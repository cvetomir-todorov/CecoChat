using CecoChat.Kafka;

namespace CecoChat.Server.State.Backplane
{
    public sealed class BackplaneOptions
    {
        public KafkaOptions Kafka { get; set; }

        public KafkaConsumerOptions ReceiversConsumer { get; set; }

        public KafkaConsumerOptions SendersConsumer { get; set; }

        public string TopicMessagesByReceiver { get; set; }

        public string TopicMessagesBySender { get; set; }
    }
}