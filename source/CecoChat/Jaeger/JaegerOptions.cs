using OpenTelemetry;

namespace CecoChat.Jaeger;

public sealed class JaegerOptions
{
    public string AgentHost { get; set; } = string.Empty;

    public int AgentPort { get; set; }

    public ExportProcessorType ExportProcessorType { get; set; }

    public int BatchExportScheduledDelayMillis { get; set; }
}
