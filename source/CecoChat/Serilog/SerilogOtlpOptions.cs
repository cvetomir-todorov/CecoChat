using OpenTelemetry.Exporter;

namespace CecoChat.Serilog;

public sealed class SerilogOtlpOptions
{
    public string TargetHost { get; init; } = string.Empty;

    public int TargetPort { get; init; }

    public OtlpExportProtocol Protocol { get; init; }

    public TimeSpan BatchPeriod { get; init; }

    public int BatchSizeLimit { get; init; }

    public int BatchQueueLimit { get; init; }
}
