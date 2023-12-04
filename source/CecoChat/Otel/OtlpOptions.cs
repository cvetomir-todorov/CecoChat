using OpenTelemetry;
using OpenTelemetry.Exporter;

namespace CecoChat.Otel;

public sealed class OtlpOptions
{
    public string TargetHost { get; init; } = string.Empty;

    public int TargetPort { get; init; }

    public OtlpExportProtocol Protocol { get; init; }

    public ExportProcessorType ExportProcessorType { get; init; }

    public int BatchExportScheduledDelayMillis { get; init; }
}
