using CecoChat.Kafka;

namespace CecoChat.Materialize.Server.Backend
{
    public interface IBackendOptions
    {
        public IKafkaOptions Kafka { get; }

        public IKafkaConsumerOptions MaterializeConsumer { get; }

        public string MessagesTopicName { get; }
    }

    public sealed class BackendOptions : IBackendOptions
    {
        public KafkaOptions Kafka { get; set; }

        public KafkaConsumerOptions MaterializeConsumer { get; set; }

        IKafkaOptions IBackendOptions.Kafka => Kafka;

        IKafkaConsumerOptions IBackendOptions.MaterializeConsumer => MaterializeConsumer;

        public string MessagesTopicName { get; set; }
    }
}
