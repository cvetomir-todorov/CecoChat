using CecoChat.Kafka;

namespace CecoChat.Messaging.Server.Backplane
{
    public interface IBackplaneOptions
    {
        public IKafkaOptions Kafka { get; }

        public IKafkaProducerOptions SendProducer { get; }

        public IKafkaConsumerOptions ReceiversConsumer { get; }

        public string ServerID { get; }

        public string MessagesTopicName { get; }
    }

    public sealed class BackplaneOptions : IBackplaneOptions
    {
        public KafkaOptions Kafka { get; set; }

        public KafkaProducerOptions SendProducer { get; set; }

        public KafkaConsumerOptions ReceiversConsumer { get; set; }

        IKafkaOptions IBackplaneOptions.Kafka => Kafka;

        IKafkaProducerOptions IBackplaneOptions.SendProducer => SendProducer;

        IKafkaConsumerOptions IBackplaneOptions.ReceiversConsumer => ReceiversConsumer;

        public string ServerID { get; set; }

        public string MessagesTopicName { get; set; }
    }
}
