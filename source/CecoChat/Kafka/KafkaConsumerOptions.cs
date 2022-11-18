using Confluent.Kafka;

namespace CecoChat.Kafka;

public sealed class KafkaConsumerOptions
{
    public string ConsumerGroupId { get; set; } = string.Empty;

    public AutoOffsetReset AutoOffsetReset { get; set; }

    public bool EnablePartitionEof { get; set; }

    public bool AllowAutoCreateTopics { get; set; }

    public bool EnableAutoCommit { get; set; }
}
