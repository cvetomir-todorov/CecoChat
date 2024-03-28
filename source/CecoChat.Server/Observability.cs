using CecoChat.Config.Client;
using Common.AspNet.Health;
using Common.AspNet.Prometheus;
using Common.Health;
using Common.Http.Health;
using Common.Kafka;
using Common.Kafka.Health;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;

namespace CecoChat.Server;

public static class TelemetryExtensions
{
    public static TracerProviderBuilder AddAspNetCoreServer(
        this TracerProviderBuilder tracing,
        PrometheusOptions prometheusOptions)
    {
        return tracing.AddAspNetCoreInstrumentation(aspnet =>
        {
            HashSet<string> excludedPaths = new()
            {
                prometheusOptions.ScrapeEndpointPath, HealthPaths.Health, HealthPaths.Startup, HealthPaths.Live, HealthPaths.Ready
            };
            aspnet.Filter = httpContext => !excludedPaths.Contains(httpContext.Request.Path);
        });
    }

    public static void EnableGrpcInstrumentationForAspNet(this WebApplicationBuilder builder)
    {
        KeyValuePair<string, string?>[] pairs =
        [
            new("OTEL_DOTNET_EXPERIMENTAL_ASPNETCORE_ENABLE_GRPC_INSTRUMENTATION", "true")
        ];
        builder.Configuration.AddInMemoryCollection(pairs);
    }
}

public static class HealthExtensions
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
        string name = "backplane")
    {
        BackplaneOptions backplaneOptions = new();
        configuration.Bind(backplaneOptions);

        return builder.AddKafka(
            name,
            backplaneOptions.Kafka,
            backplaneOptions.Health,
            tags: new[] { HealthTags.Health, HealthTags.Ready });
    }

    public static void MapCustomHttpHealthEndpoints(
        this IEndpointRouteBuilder endpoints,
        IWebHostEnvironment environment,
        string serviceName)
    {
        endpoints.MapHttpHealthEndpoints(setup =>
        {
            Func<HttpContext, HealthReport, Task> responseWriter = (context, report) => CustomHealth.Writer(serviceName, context, report);
            setup.Health.ResponseWriter = responseWriter;

            if (environment.IsDevelopment())
            {
                setup.Startup.ResponseWriter = responseWriter;
                setup.Live.ResponseWriter = responseWriter;
                setup.Ready.ResponseWriter = responseWriter;
            }
        });
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
