using Confluent.Kafka;

namespace CecoChat.Kafka
{
    public interface IKafkaConsumerOptions
    {
        string ConsumerGroupID { get; set; }

        AutoOffsetReset AutoOffsetReset { get; set; }

        bool EnablePartitionEof { get; set; }

        bool AllowAutoCreateTopics { get; set; }

        bool EnableAutoCommit { get; set; }
    }

    public sealed class KafkaConsumerOptions : IKafkaConsumerOptions
    {
        public string ConsumerGroupID { get; set; }

        public AutoOffsetReset AutoOffsetReset { get; set; }

        public bool EnablePartitionEof { get; set; }

        public bool AllowAutoCreateTopics { get; set; }

        public bool EnableAutoCommit { get; set; }
    }
}
