using CecoChat.AspNet.Health;
using CecoChat.Client.Config;
using CecoChat.Health;
using CecoChat.Http.Health;
using CecoChat.Kafka;
using CecoChat.Kafka.Health;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Server;

public static class HealthRegistrations
{
    public static IHealthChecksBuilder AddDynamicConfigInit(
        this IHealthChecksBuilder builder,
        string name = "dynamic-config-init")
    {
        builder.Services.AddSingleton<DynamicConfigInitHealthCheck>();

        return builder.AddCheck<DynamicConfigInitHealthCheck>(
            name,
            tags: new[] { HealthTags.Health, HealthTags.Startup });
    }

    public static IHealthChecksBuilder AddConfigChangesConsumer(
        this IHealthChecksBuilder builder,
        string name = "config-changes-consumer")
    {
        builder.Services.AddSingleton<ConfigChangesConsumerHealthCheck>();

        return builder.AddCheck<ConfigChangesConsumerHealthCheck>(
            name,
            tags: new[] { HealthTags.Health, HealthTags.Startup, HealthTags.Live });
    }

    public static IHealthChecksBuilder AddConfigService(
        this IHealthChecksBuilder builder,
        ConfigClientOptions configClientOptions,
        string name = "config-svc")
    {
        return builder.AddUri(
            name,
            new Uri(configClientOptions.Address!, configClientOptions.HealthPath),
            configureHttpClient: (_, client) => client.DefaultRequestVersion = new Version(2, 0),
            timeout: configClientOptions.HealthTimeout,
            tags: new[] { HealthTags.Health, HealthTags.Ready });
    }

    public static IHealthChecksBuilder AddBackplane(
        this IHealthChecksBuilder builder,
        IConfiguration configuration,
        string configSectionName = "Backplane",
        string healthCheckName = "backplane")
    {
        BackplaneOptions backplaneOptions = new();
        configuration.GetSection(configSectionName).Bind(backplaneOptions);

        return builder.AddKafka(
            healthCheckName,
            backplaneOptions.Kafka,
            backplaneOptions.Health,
            tags: new[] { HealthTags.Health, HealthTags.Ready });
    }
}

public class DynamicConfigInitHealthCheck : StatusHealthCheck
{ }

public class ConfigChangesConsumerHealthCheck : StatusHealthCheck
{ }

internal sealed class BackplaneOptions
{
    public KafkaOptions Kafka { get; init; } = new();

    public KafkaHealthOptions Health { get; init; } = new();
}
