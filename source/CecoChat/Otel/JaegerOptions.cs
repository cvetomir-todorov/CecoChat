using OpenTelemetry;

namespace CecoChat.Otel
{
    public interface IJaegerOptions
    {
        string AgentHost { get; }

        int AgentPort { get; }

        ExportProcessorType ExportProcessorType { get; }

        int BatchExportScheduledDelayMillis{ get; }
    }

    public sealed class JaegerOptions : IJaegerOptions
    {
        public string AgentHost { get; set; }

        public int AgentPort { get; set; }

        public ExportProcessorType ExportProcessorType { get; set; }

        public int BatchExportScheduledDelayMillis { get; set; }
    }
}
