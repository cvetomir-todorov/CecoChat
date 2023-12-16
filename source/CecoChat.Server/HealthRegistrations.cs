using CecoChat.AspNet.Health;
using CecoChat.Health;
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
}

public class DynamicConfigInitHealthCheck : StatusHealthCheck
{ }

public class ConfigChangesConsumerHealthCheck : StatusHealthCheck
{ }
