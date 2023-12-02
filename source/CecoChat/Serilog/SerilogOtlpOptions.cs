using OpenTelemetry.Exporter;

namespace CecoChat.Serilog;

public sealed class SerilogOtlpOptions
{
    public string TargetHost { get; init; } = "localhost";

    public int TargetPort { get; init; } = 4317;

    public OtlpExportProtocol Protocol { get; init; }

    public TimeSpan BatchPeriod { get; init; }

    public int BatchSizeLimit { get; init; }

    public int BatchQueueLimit { get; init; }
}
