using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Common.AspNet.Health;

public sealed class HttpHealthEndpoint
{
    public bool Enabled { get; set; } = true;

    public string Pattern { get; set; } = string.Empty;

    public Func<HealthCheckRegistration, bool>? Predicate { get; set; }

    public Dictionary<HealthStatus, int> ResultStatusCodes { get; set; } = new()
    {
        { HealthStatus.Healthy, (int)HttpStatusCode.OK },
        { HealthStatus.Degraded, (int)HttpStatusCode.OK },
        { HealthStatus.Unhealthy, (int)HttpStatusCode.ServiceUnavailable }
    };

    public Func<HttpContext, HealthReport, Task>? ResponseWriter { get; set; }
}
