using OpenTelemetry.Metrics;

namespace CecoChat.AspNet.Otel;

public static class OtelRegistrations
{
    public static MeterProviderBuilder ConfigurePrometheusAspNetExporter(this MeterProviderBuilder metrics, PrometheusOptions options)
    {
        return metrics
            .AddPrometheusExporter(prometheus =>
            {
                prometheus.ScrapeEndpointPath = options.ScrapeEndpointPath;
                prometheus.ScrapeResponseCacheDurationMilliseconds = options.ScrapeResponseCacheDurationMilliseconds;
            });
    }
}
