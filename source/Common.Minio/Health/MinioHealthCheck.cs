using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

namespace Common.Minio.Health;

/// <summary>
/// Checks the health of MinIO by checking if a bucket exists.
/// </summary>
public class MinioHealthCheck : IHealthCheck
{
    private readonly ILogger _logger;
    private readonly IMinioClient _minio;
    private readonly string _bucket;

    public MinioHealthCheck(
        ILogger<MinioHealthCheck> logger,
        IMinioClient minio,
        string bucket)
    {
        _logger = logger;
        _minio = minio;
        _bucket = bucket;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            BucketExistsArgs bucketExistsArgs = new BucketExistsArgs()
                .WithBucket(_bucket);
            await _minio.BucketExistsAsync(bucketExistsArgs, cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "MinIO health check {HealthCheckName} failed", context.Registration.Name);
            return new HealthCheckResult(HealthStatus.Unhealthy, exception.Message, exception);
        }
    }
}
