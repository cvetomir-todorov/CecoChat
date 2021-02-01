using CecoChat.Kafka;

namespace CecoChat.Messaging.Server.Backend
{
    public interface IBackendOptions
    {
        public IKafkaOptions Kafka { get; }

        public string MessagesTopicName { get; }

        public int MessagesTopicPartitionCount { get; }
    }

    public sealed class BackendOptions : IBackendOptions
    {
        public KafkaOptions Kafka { get; set; }

        IKafkaOptions IBackendOptions.Kafka => Kafka;

        public string MessagesTopicName { get; set; }

        public int MessagesTopicPartitionCount { get; set; }
    }
}
