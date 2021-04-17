using CecoChat.Kafka;

namespace CecoChat.Messaging.Server.Backend
{
    public interface IBackendOptions
    {
        public IKafkaOptions Kafka { get; }

        public IKafkaProducerOptions MessagesToBackendProducer { get; }

        public IKafkaConsumerOptions MessagesToReceiversConsumer { get; }

        public string ServerID { get; }

        public string MessagesTopicName { get; }
    }

    public sealed class BackendOptions : IBackendOptions
    {
        public KafkaOptions Kafka { get; set; }

        public KafkaProducerOptions MessagesToBackendProducer { get; set; }

        public KafkaConsumerOptions MessagesToReceiversConsumer { get; set; }

        IKafkaOptions IBackendOptions.Kafka => Kafka;

        IKafkaProducerOptions IBackendOptions.MessagesToBackendProducer => MessagesToBackendProducer;

        IKafkaConsumerOptions IBackendOptions.MessagesToReceiversConsumer => MessagesToReceiversConsumer;

        public string ServerID { get; set; }

        public string MessagesTopicName { get; set; }
    }
}
