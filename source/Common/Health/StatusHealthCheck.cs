using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Common.Health;

public class StatusHealthCheck : IHealthCheck
{
    private volatile bool _isReady;

    public bool IsReady
    {
        get => _isReady;
        set => _isReady = value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (IsReady)
        {
            return Task.FromResult(HealthCheckResult.Healthy());
        }
        else
        {
            return Task.FromResult(HealthCheckResult.Unhealthy());
        }
    }
}
