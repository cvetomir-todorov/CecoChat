using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace CecoChat.Otel;

public static class OtlpRegistrations
{
    public static TracerProviderBuilder ConfigureOtlpExporter(this TracerProviderBuilder tracing, OtlpOptions options)
    {
        return tracing.AddOtlpExporter(otlp => SetOtlpOptions(otlp, options));
    }

    public static MeterProviderBuilder ConfigureOtlpExporter(this MeterProviderBuilder metrics, OtlpOptions options)
    {
        return metrics.AddOtlpExporter(otlp => SetOtlpOptions(otlp, options));
    }

    private static void SetOtlpOptions(OtlpExporterOptions otlp, OtlpOptions options)
    {
        otlp.Endpoint = new Uri($"http://{options.TargetHost}:{options.TargetPort}");
        otlp.Protocol = options.Protocol;
        otlp.ExportProcessorType = options.ExportProcessorType;
        otlp.BatchExportProcessorOptions.ScheduledDelayMilliseconds = options.BatchExportScheduledDelayMillis;
    }
}
