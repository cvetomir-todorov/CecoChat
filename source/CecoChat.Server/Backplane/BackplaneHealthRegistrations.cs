using CecoChat.Kafka;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Server.Backplane;

public static class BackplaneHealthRegistrations
{
    /// <summary>
    /// Adds a health check for Kafka backplane with a default name and a default <paramref name="timeout"/> set to 5 seconds.
    /// </summary>
    public static IHealthChecksBuilder AddBackplane(
        this IHealthChecksBuilder healthChecks,
        KafkaOptions options,
        KafkaProducerOptions producerOptions,
        string topic,
        string name = "backplane",
        string[]? tags = null,
        TimeSpan? timeout = null)
    {
        return healthChecks.AddKafka(config =>
            {
                config.BootstrapServers = string.Join(separator: ',', options.BootstrapServers);
                config.Acks = producerOptions.Acks;
                config.LingerMs = producerOptions.LingerMs;
                config.MessageSendMaxRetries = producerOptions.MessageSendMaxRetries;
                config.MessageTimeoutMs = producerOptions.MessageTimeoutMs;
            },
            name: name,
            topic: topic,
            tags: tags,
            timeout: timeout);
    }
}
