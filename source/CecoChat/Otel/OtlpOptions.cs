using OpenTelemetry;
using OpenTelemetry.Exporter;

namespace CecoChat.Otel;

public sealed class OtlpOptions
{
    public string TargetHost { get; init; } = "localhost";

    public int TargetPort { get; init; } = 4317;

    public OtlpExportProtocol Protocol { get; init; } = OtlpExportProtocol.Grpc;

    public ExportProcessorType ExportProcessorType { get; init; } = ExportProcessorType.Batch;

    public int BatchExportScheduledDelayMillis { get; init; } = 1000;
}
