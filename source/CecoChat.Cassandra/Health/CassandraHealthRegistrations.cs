using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

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
            serviceProvider => CreateHealthCheck(serviceProvider, timeout),
            failureStatus,
            tags,
            timeout));
    }

    private static IHealthCheck CreateHealthCheck(IServiceProvider serviceProvider, TimeSpan? timeout)
    {
        return new CassandraHealthCheck(
            serviceProvider.GetRequiredService<ILogger<CassandraHealthCheck>>(),
            serviceProvider.GetRequiredService<ICassandraHealthDbContext>(),
            timeout);
    }
}
