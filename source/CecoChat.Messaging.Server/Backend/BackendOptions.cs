using CecoChat.Kafka;

namespace CecoChat.Messaging.Server.Backend
{
    public interface IBackendOptions
    {
        public IKafkaOptions Kafka { get; }

        public IKafkaProducerOptions MessagesToBackendProducer { get; }

        public IKafkaConsumerOptions MessagesToReceiversConsumer { get; }

        public IKafkaConsumerOptions MessagesToSendersConsumer { get; }

        public string ServerID { get; }

        public string MessagesTopicName { get; }
    }

    public sealed class BackendOptions : IBackendOptions
    {
        public KafkaOptions Kafka { get; set; }

        public KafkaProducerOptions MessagesToBackendProducer { get; set; }

        public KafkaConsumerOptions MessagesToReceiversConsumer { get; set; }

        public KafkaConsumerOptions MessagesToSendersConsumer { get; set; }

        IKafkaOptions IBackendOptions.Kafka => Kafka;

        IKafkaProducerOptions IBackendOptions.MessagesToBackendProducer => MessagesToBackendProducer;

        IKafkaConsumerOptions IBackendOptions.MessagesToReceiversConsumer => MessagesToReceiversConsumer;

        IKafkaConsumerOptions IBackendOptions.MessagesToSendersConsumer => MessagesToSendersConsumer;

        public string ServerID { get; set; }

        public string MessagesTopicName { get; set; }
    }
}
