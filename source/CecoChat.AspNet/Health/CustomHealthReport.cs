using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CecoChat.AspNet.Health;

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
    public string? ServiceName { get; set; }

    public string? ServiceVersion { get; set; }

    public string? Runtime { get; set; }

    public HealthStatus Status { get; set; }

    public TimeSpan Duration { get; set; } = TimeSpan.Zero;

    public CustomHealthDependencyReport[] Dependencies { get; set; } = Array.Empty<CustomHealthDependencyReport>();
}

public sealed class CustomHealthDependencyReport
{
    public string? Name { get; set; }

    public HealthStatus Status { get; set; }

    public TimeSpan Duration { get; set; } = TimeSpan.Zero;
}
