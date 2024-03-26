namespace Common.Kafka.Health;

public sealed class KafkaHealthOptions
{
    public KafkaProducerOptions Producer { get; init; } = new();

    public string Topic { get; init; } = string.Empty;

    public TimeSpan Timeout { get; init; }
}
