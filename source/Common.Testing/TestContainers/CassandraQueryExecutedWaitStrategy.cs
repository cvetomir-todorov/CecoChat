using Cassandra;
using Common.Cassandra;
using Common.Testing.Logging;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Common.Testing.TestContainers;

public class CassandraQueryExecutedWaitStrategy : IWaitUntil
{
    private readonly int _port;
    private readonly string _localDc;
    private readonly TimeSpan _showErrorInterval;
    private DateTime _lastShowError;

    public CassandraQueryExecutedWaitStrategy(int port, string localDc, TimeSpan showErrorInterval)
    {
        _port = port;
        _localDc = localDc;
        _showErrorInterval = showErrorInterval;
    }

    public Task<bool> UntilAsync(IContainer container)
    {
        CassandraOptions options = new()
        {
            ContactPoints = [$"{container.Hostname}:{_port}"],
            LocalDc = _localDc,
            SocketConnectTimeout = TimeSpan.FromSeconds(5),
            ExponentialReconnectPolicy = true,
            ExponentialReconnectPolicyBaseDelay = TimeSpan.FromMilliseconds(500),
            ExponentialReconnectPolicyMaxDelay = TimeSpan.FromSeconds(5)
        };

        TestCassandraDbContext? db = null;

        try
        {
            db = new(new NUnitProgressLogger<CassandraDbContext>(), new OptionsWrapper<CassandraOptions>(options));
            // we simply want to make sure that there is connection to the cassandra cluster
            // we don't really care if this keyspace actually exists
            db.ExistsKeyspace("non-existing");

            return Task.FromResult(true);
        }
        catch (NoHostAvailableException noHostAvailableException)
        {
            DateTime currentShowError = DateTime.UtcNow;
            TimeSpan timeSinceLastShowError = currentShowError - _lastShowError;

            if (timeSinceLastShowError > _showErrorInterval)
            {
                container.Logger.LogError(
                    noHostAvailableException,
                    "No connection to Cassandra at {Host}:{Port} (showing this error once each {ShowErrorInterval} secs)",
                    container.Hostname, _port, Math.Ceiling(_showErrorInterval.TotalSeconds));
                _lastShowError = currentShowError;
            }

            return Task.FromResult(false);
        }
        finally
        {
            db?.Dispose();
        }
    }

    private class TestCassandraDbContext : CassandraDbContext
    {
        public TestCassandraDbContext(ILogger<CassandraDbContext> logger, IOptions<CassandraOptions> options) : base(logger, options)
        { }
    }
}

public static class WaitForContainerOsExtensions
{
    public static IWaitForContainerOS UntilCassandraQueryExecuted(this IWaitForContainerOS waitFor, int port, string localDc, TimeSpan showErrorInterval)
    {
        return waitFor.AddCustomWaitStrategy(new CassandraQueryExecutedWaitStrategy(port, localDc, showErrorInterval));
    }
}
