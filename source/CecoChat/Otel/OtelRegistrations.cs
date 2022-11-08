using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace CecoChat.Otel;

public static class OtelRegistrations
{
    public static TracerProviderBuilder ConfigureSampling(this TracerProviderBuilder tracing, OtelSamplingOptions samplingOptions)
    {
        Sampler sampler;
        switch (samplingOptions.Strategy)
        {
            case OtelSamplingStrategy.AlwaysOff:
                sampler = new AlwaysOffSampler();
                break;
            case OtelSamplingStrategy.AlwaysOn:
                sampler = new AlwaysOnSampler();
                break;
            case OtelSamplingStrategy.Probability:
                sampler = new CustomSampler(samplingOptions.Probability);
                break;
            default:
                throw new EnumValueNotSupportedException(samplingOptions.Strategy);
        }

        return tracing.SetSampler(sampler);
    }

    public static TracerProviderBuilder ConfigureJaegerExporter(this TracerProviderBuilder tracing, JaegerOptions options)
    {
        return tracing
            .AddJaegerExporter(jaeger =>
            {
                jaeger.AgentHost = options.AgentHost;
                jaeger.AgentPort = options.AgentPort;
                jaeger.ExportProcessorType = options.ExportProcessorType;
                jaeger.BatchExportProcessorOptions.ScheduledDelayMilliseconds = options.BatchExportScheduledDelayMillis;
            });
    }

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
