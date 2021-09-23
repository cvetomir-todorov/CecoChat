using Confluent.Kafka;

namespace CecoChat.Kafka
{
    public sealed class KafkaConsumerOptions
    {
        public string ConsumerGroupID { get; set; }

        public AutoOffsetReset AutoOffsetReset { get; set; }

        public bool EnablePartitionEof { get; set; }

        public bool AllowAutoCreateTopics { get; set; }

        public bool EnableAutoCommit { get; set; }
    }
}
