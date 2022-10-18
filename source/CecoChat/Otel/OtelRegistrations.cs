using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CecoChat.Otel;

public static class OtelRegistrations
{
    public static TracerProviderBuilder AddServiceResource(this TracerProviderBuilder otel, OtelServiceResource serviceResource)
    {
        ResourceBuilder resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(
                serviceNamespace: serviceResource.Namespace,
                serviceName: serviceResource.Name,
                serviceVersion: serviceResource.Version);

        return otel.SetResourceBuilder(resourceBuilder);
    }

    public static TracerProviderBuilder ConfigureSampling(this TracerProviderBuilder otel, OtelSamplingOptions samplingOptions)
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
                throw new InvalidOperationException($"{typeof(OtelSamplingStrategy)} value {samplingOptions.Strategy} is not valid.");
        }

        return otel.SetSampler(sampler);
    }

    public static TracerProviderBuilder ConfigureJaegerExporter(this TracerProviderBuilder otel, JaegerOptions options)
    {
        return otel
            .AddJaegerExporter(config =>
            {
                config.AgentHost = options.AgentHost;
                config.AgentPort = options.AgentPort;
                config.ExportProcessorType = options.ExportProcessorType;
                config.BatchExportProcessorOptions.ScheduledDelayMilliseconds = options.BatchExportScheduledDelayMillis;
            });
    }
}