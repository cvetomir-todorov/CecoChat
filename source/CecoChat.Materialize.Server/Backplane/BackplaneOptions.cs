using CecoChat.Kafka;

namespace CecoChat.Materialize.Server.Backplane
{
    public interface IBackplaneOptions
    {
        public IKafkaOptions Kafka { get; }

        public IKafkaConsumerOptions MaterializeConsumer { get; }

        public string MessagesTopicName { get; }
    }

    public sealed class BackplaneOptions : IBackplaneOptions
    {
        public KafkaOptions Kafka { get; set; }

        public KafkaConsumerOptions MaterializeConsumer { get; set; }

        IKafkaOptions IBackplaneOptions.Kafka => Kafka;

        IKafkaConsumerOptions IBackplaneOptions.MaterializeConsumer => MaterializeConsumer;

        public string MessagesTopicName { get; set; }
    }
}
