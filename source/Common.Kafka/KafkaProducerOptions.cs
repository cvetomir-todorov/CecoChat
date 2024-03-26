using Confluent.Kafka;

namespace Common.Kafka;

public sealed class KafkaProducerOptions
{
    public string ProducerId { get; init; } = string.Empty;

    public Acks Acks { get; init; }

    public double LingerMs { get; init; }

    public int MessageTimeoutMs { get; init; }

    public int MessageSendMaxRetries { get; init; }
}
