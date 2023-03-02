using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CecoChat.Kafka.Health;

public static class KafkaHealthRegistrations
{
    public static IHealthChecksBuilder AddKafka(
        this IHealthChecksBuilder healthChecks,
        string name,
        KafkaOptions options,
        KafkaProducerOptions producerOptions,
        string topic,
        string[]? tags = null,
        TimeSpan? timeout = null,
        HealthStatus failureStatus = HealthStatus.Unhealthy)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException($"Argument {nameof(topic)} should not be null or whitespace.", nameof(topic));
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"Argument {nameof(name)} should not be null or whitespace.", nameof(name));
        }

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
            timeout: timeout,
            failureStatus: failureStatus);
    }
}
