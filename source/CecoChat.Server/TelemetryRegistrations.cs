using CecoChat.AspNet.Health;
using CecoChat.AspNet.Prometheus;
using OpenTelemetry.Trace;

namespace CecoChat.Server;

public static class TelemetryRegistrations
{
    public static TracerProviderBuilder AddAspNetCoreServer(
        this TracerProviderBuilder tracing,
        bool enableGrpcSupport,
        PrometheusOptions prometheusOptions)
    {
        return tracing.AddAspNetCoreInstrumentation(aspnet =>
        {
            aspnet.EnableGrpcAspNetCoreSupport = enableGrpcSupport;

            HashSet<string> excludedPaths = new()
            {
                prometheusOptions.ScrapeEndpointPath, HealthPaths.Health, HealthPaths.Startup, HealthPaths.Live, HealthPaths.Ready
            };
            aspnet.Filter = httpContext => !excludedPaths.Contains(httpContext.Request.Path);
        });
    }
}
