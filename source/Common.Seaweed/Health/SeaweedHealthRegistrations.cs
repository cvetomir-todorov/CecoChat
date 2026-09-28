using Common.Http.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Common.Seaweed.Health;

public static class SeaweedHealthRegistrations
{
    /// <summary>
    /// Checks the health of SeaweedFS by invoking the S3 health endpoint.
    /// </summary>
    public static IHealthChecksBuilder AddSeaweed(
        this IHealthChecksBuilder builder,
        string name,
        SeaweedOptions options,
        HealthStatus failureStatus = HealthStatus.Unhealthy,
        IEnumerable<string>? tags = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return builder.AddUri(
            name,
            new Uri(options.Endpoint, options.Health.Path),
            failureStatus: failureStatus,
            tags: tags,
            timeout: options.Health.Timeout);
    }
}
