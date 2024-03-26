using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Common.AspNet.Health;

/// <summary>
/// End result:
/// 
/// {
///   "ServiceName": "history",
///   "ServiceVersion": "1.0.0.0",
///   "Runtime": ".NET 6.0.10",
///   "Status": "Healthy",
///   "Duration": "00:00:01.234567",
///   "Dependencies": [
///     {
///       "Name": "backplane",
///       "Status": "Healthy"
///       "Duration": "00:00:01.234567",
///     },
///     {
///       "Name": "history-db",
///       "Status": "Healthy",
///       "Duration": "00:00:01.234567",
///     }
///   ]
/// }
/// </summary>
public sealed class CustomHealthReport
{
    public string ServiceName { get; init; } = string.Empty;

    public string? ServiceVersion { get; init; }

    public string Runtime { get; init; } = string.Empty;

    public HealthStatus Status { get; init; }

    public TimeSpan Duration { get; init; } = TimeSpan.Zero;

    public CustomHealthDependencyReport[] Dependencies { get; init; } = Array.Empty<CustomHealthDependencyReport>();
}

public sealed class CustomHealthDependencyReport
{
    public string? Name { get; init; }

    public HealthStatus Status { get; init; }

    public TimeSpan Duration { get; init; } = TimeSpan.Zero;
}
