using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Common.Kafka.Health;

public static class KafkaHealthRegistrations
{
    public static IHealthChecksBuilder AddKafka(
        this IHealthChecksBuilder healthChecks,
        string name,
        KafkaOptions options,
        KafkaHealthOptions healthOptions,
        string[]? tags = null,
        HealthStatus failureStatus = HealthStatus.Unhealthy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"Argument {nameof(name)} should not be null or whitespace.", nameof(name));
        }

        return healthChecks.AddKafka(config =>
            {
                config.BootstrapServers = string.Join(separator: ',', options.BootstrapServers);
                config.Acks = healthOptions.Producer.Acks;
                config.LingerMs = healthOptions.Producer.LingerMs;
                config.MessageSendMaxRetries = healthOptions.Producer.MessageSendMaxRetries;
                config.MessageTimeoutMs = healthOptions.Producer.MessageTimeoutMs;
            },
            name: name,
            topic: healthOptions.Topic,
            tags: tags,
            timeout: healthOptions.Timeout,
            failureStatus: failureStatus);
    }
}
