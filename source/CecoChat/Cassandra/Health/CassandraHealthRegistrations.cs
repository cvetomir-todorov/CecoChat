using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CecoChat.Cassandra.Health;

public static class CassandraHealthRegistrations
{
    public static IHealthChecksBuilder AddCassandra(
        this IHealthChecksBuilder builder,
        string name,
        HealthStatus failureStatus = HealthStatus.Unhealthy,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"Argument {nameof(name)} should not be null or whitespace.", nameof(name));
        }

        return builder.Add(new HealthCheckRegistration(
            name,
            serviceProvider => serviceProvider.GetRequiredService<CassandraHealthCheck>(),
            failureStatus,
            tags,
            timeout));
    }
}
