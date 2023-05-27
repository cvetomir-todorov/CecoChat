namespace CecoChat.Kafka;

public sealed class KafkaOptions
{
    public string[] BootstrapServers { get; set; } = Array.Empty<string>();
}
