namespace Common.Kafka;

public sealed class KafkaOptions
{
    public string[] BootstrapServers { get; init; } = [];
}
