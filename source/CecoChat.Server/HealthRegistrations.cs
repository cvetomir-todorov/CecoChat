using CecoChat.AspNet.Health;
using CecoChat.Client.Config;
using CecoChat.Health;
using CecoChat.Http.Health;
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
}

public class DynamicConfigInitHealthCheck : StatusHealthCheck
{ }

public class ConfigChangesConsumerHealthCheck : StatusHealthCheck
{ }
