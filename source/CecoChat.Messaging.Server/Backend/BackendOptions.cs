using CecoChat.Kafka;

namespace CecoChat.Messaging.Server.Backend
{
    public interface IBackendOptions
    {
        public IKafkaOptions Kafka { get; }

        public IKafkaProducerOptions SendProducer { get; }

        public IKafkaConsumerOptions ReceiversConsumers { get; }

        public string ServerID { get; }

        public string MessagesTopicName { get; }
    }

    public sealed class BackendOptions : IBackendOptions
    {
        public KafkaOptions Kafka { get; set; }

        public KafkaProducerOptions SendProducer { get; set; }

        public KafkaConsumerOptions ReceiversConsumers { get; set; }

        IKafkaOptions IBackendOptions.Kafka => Kafka;

        IKafkaProducerOptions IBackendOptions.SendProducer => SendProducer;

        IKafkaConsumerOptions IBackendOptions.ReceiversConsumers => ReceiversConsumers;

        public string ServerID { get; set; }

        public string MessagesTopicName { get; set; }
    }
}
