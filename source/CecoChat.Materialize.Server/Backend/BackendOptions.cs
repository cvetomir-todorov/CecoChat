using CecoChat.Kafka;

namespace CecoChat.Materialize.Server.Backend
{
    public interface IBackendOptions
    {
        public IKafkaOptions Kafka { get; }

        public IKafkaConsumerOptions MaterializeMessagesConsumer { get; }

        public string MessagesTopicName { get; }
    }

    public sealed class BackendOptions : IBackendOptions
    {
        public KafkaOptions Kafka { get; set; }

        public KafkaConsumerOptions MaterializeMessagesConsumer { get; set; }

        IKafkaOptions IBackendOptions.Kafka => Kafka;

        IKafkaConsumerOptions IBackendOptions.MaterializeMessagesConsumer => MaterializeMessagesConsumer;

        public string MessagesTopicName { get; set; }
    }
}
