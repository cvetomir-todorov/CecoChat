using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CecoChat.Otel
{
    public record OtelServiceResource
    {
        public string Namespace { get; init; }
        public string Service { get; init; }
        public string Version { get; init; }
    }

    public static class OtelExtensions
    {
        public static TracerProviderBuilder AddServiceResource(this TracerProviderBuilder otel, OtelServiceResource serviceResource)
        {
            ResourceBuilder resourceBuilder = ResourceBuilder.CreateDefault()
                .AddService(
                    serviceNamespace: serviceResource.Namespace,
                    serviceName: serviceResource.Service,
                    serviceVersion: serviceResource.Version);

            return otel.SetResourceBuilder(resourceBuilder);
        }

        public static TracerProviderBuilder ConfigureJaegerExporter(this TracerProviderBuilder otel, IJaegerOptions options)
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
}
