using Cassandra;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Common.Cassandra.Health;

public class CassandraHealthCheck : IHealthCheck
{
    private readonly ILogger _logger;
    private readonly ICassandraDbContext _dbContext;
    private readonly Lazy<BoundStatement> _query;

    public CassandraHealthCheck(ILogger<CassandraHealthCheck> logger, ICassandraDbContext dbContext, TimeSpan? timeout)
    {
        _logger = logger;
        _dbContext = dbContext;
        _query = new Lazy<BoundStatement>(() =>
        {
            const string query = "SELECT now() FROM system.local";
            PreparedStatement prepared = _dbContext.GetSession().Prepare(query);

            BoundStatement bound = prepared.Bind();
            bound.SetIdempotence(true);
            bound.SetConsistencyLevel(ConsistencyLevel.Quorum);
            if (timeout.HasValue)
            {
                bound.SetReadTimeoutMillis(Convert.ToInt32(Math.Ceiling(timeout.Value.TotalMilliseconds)));
            }

            return bound;
        });
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.GetSession().ExecuteAsync(_query.Value);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Cassandra health check {HealthCheckName} failed", context.Registration.Name);
            return new HealthCheckResult(context.Registration.FailureStatus, exception.Message, exception);
        }
    }
}
