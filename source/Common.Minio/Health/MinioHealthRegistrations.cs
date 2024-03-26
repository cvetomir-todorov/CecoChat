using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Minio;

namespace Common.Minio.Health;

public static class MinioHealthRegistrations
{
    /// <summary>
    /// Checks the health of MinIO by checking if a bucket exists.
    /// </summary>
    public static IHealthChecksBuilder AddMinio(
        this IHealthChecksBuilder builder,
        string name,
        string bucket,
        HealthStatus failureStatus = HealthStatus.Unhealthy,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return builder.Add(new HealthCheckRegistration(
            name,
            sp => CreateHealthCheck(sp, bucket),
            failureStatus,
            tags,
            timeout));
    }

    private static IHealthCheck CreateHealthCheck(IServiceProvider serviceProvider, string bucket)
    {
        return new MinioHealthCheck(
            serviceProvider.GetRequiredService<ILogger<MinioHealthCheck>>(),
            serviceProvider.GetRequiredService<IMinioClient>(),
            bucket);
    }
}
