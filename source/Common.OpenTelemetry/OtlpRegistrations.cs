using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Common.OpenTelemetry;

public static class OtlpRegistrations
{
    public static TracerProviderBuilder ConfigureOtlpExporter(this TracerProviderBuilder tracing, OtlpExporterOptions options)
    {
        return tracing.AddOtlpExporter(otlp => SetOtlpOptions(otlp, options));
    }

    public static MeterProviderBuilder ConfigureOtlpExporter(this MeterProviderBuilder metrics, OtlpExporterOptions options)
    {
        return metrics.AddOtlpExporter(otlp => SetOtlpOptions(otlp, options));
    }

    private static void SetOtlpOptions(global::OpenTelemetry.Exporter.OtlpExporterOptions otlp, OtlpExporterOptions options)
    {
        otlp.Endpoint = new Uri($"http://{options.TargetHost}:{options.TargetPort}");
        otlp.Protocol = options.Protocol;
        otlp.ExportProcessorType = options.ExportProcessorType;
        otlp.BatchExportProcessorOptions.ScheduledDelayMilliseconds = options.BatchExportScheduledDelayMillis;
    }
}
