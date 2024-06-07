using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Common.Cassandra.Health;

public static class CassandraHealthRegistrations
{
    public static IHealthChecksBuilder AddCassandra<TDbContext>(
        this IHealthChecksBuilder builder,
        string name,
        HealthStatus failureStatus = HealthStatus.Unhealthy,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
        where TDbContext : ICassandraDbContext
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"Argument {nameof(name)} should not be null or whitespace.", nameof(name));
        }

        return builder.Add(new HealthCheckRegistration(
            name,
            serviceProvider => CreateHealthCheck<TDbContext>(serviceProvider, timeout),
            failureStatus,
            tags,
            timeout));
    }

    private static IHealthCheck CreateHealthCheck<TDbContext>(IServiceProvider serviceProvider, TimeSpan? timeout)
        where TDbContext : ICassandraDbContext
    {
        return new CassandraHealthCheck(
            serviceProvider.GetRequiredService<ILogger<CassandraHealthCheck>>(),
            serviceProvider.GetRequiredService<TDbContext>(),
            timeout);
    }
}
