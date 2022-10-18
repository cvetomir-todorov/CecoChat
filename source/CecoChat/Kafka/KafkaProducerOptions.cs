using Confluent.Kafka;

namespace CecoChat.Kafka;

public sealed class KafkaProducerOptions
{
    public string ProducerID { get; set; }

    public Acks Acks { get; set; }

    public double LingerMs { get; set; }

    public int MessageTimeoutMs { get; set; }

    public int MessageSendMaxRetries { get; set; }
}