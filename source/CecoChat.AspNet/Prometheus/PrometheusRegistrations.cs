using OpenTelemetry.Metrics;

namespace CecoChat.AspNet.Prometheus;

public static class PrometheusRegistrations
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
