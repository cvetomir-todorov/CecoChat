using CecoChat.Kafka;

namespace CecoChat.Materialize.Server.Backend
{
    public interface IBackendOptions
    {
        public IKafkaOptions Kafka { get; }

        public string MessagesTopicName { get; }
    }

    public sealed class BackendOptions : IBackendOptions
    {
        public KafkaOptions Kafka { get; set; }

        IKafkaOptions IBackendOptions.Kafka => Kafka;

        public string MessagesTopicName { get; set; }
    }
}
