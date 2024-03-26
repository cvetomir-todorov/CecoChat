using Confluent.Kafka;

namespace Common.Kafka;

public sealed class KafkaConsumerOptions
{
    public string ConsumerGroupId { get; set; } = string.Empty;

    public AutoOffsetReset AutoOffsetReset { get; init; }

    public bool EnablePartitionEof { get; init; }

    public bool AllowAutoCreateTopics { get; init; }

    public bool EnableAutoCommit { get; init; }
}
