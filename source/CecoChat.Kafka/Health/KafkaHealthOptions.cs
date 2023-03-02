namespace CecoChat.Kafka.Health;

public sealed class KafkaHealthOptions
{
    public KafkaProducerOptions Producer { get; set; } = new();

    public string Topic { get; set; } = string.Empty;

    public TimeSpan Timeout { get; set; }
}
