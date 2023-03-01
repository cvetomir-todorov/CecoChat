using System.Collections.Concurrent;
using Cassandra;
using Microsoft.Extensions.Options;

namespace CecoChat.Cassandra;

public interface ICassandraDbContext : IDisposable
{
    ISession GetSession();

    ISession GetSession(string keyspace);

    bool ExistsKeyspace(string keyspace);
}

public class CassandraDbContext : ICassandraDbContext
{
    private readonly CassandraOptions _options;
    private readonly Lazy<Cluster> _cluster;
    private readonly ConcurrentDictionary<string, ISession> _sessions;

    public CassandraDbContext(IOptions<CassandraOptions> options)
    {
        _options = options.Value;
        _cluster = new Lazy<Cluster>(CreateCluster);
        _sessions = new ConcurrentDictionary<string, ISession>();
    }

    private Cluster CreateCluster()
    {
        Builder clusterBuilder = new Builder()
            .AddContactPoints(_options.ContactPoints)
            .WithCompression(CompressionType.LZ4)
            .WithLoadBalancingPolicy(new DCAwareRoundRobinPolicy(_options.LocalDC));

        if (_options.SocketConnectTimeout > TimeSpan.Zero)
        {
            // currently used because of 5 seconds delay as a result from docker NAT
            SocketOptions socketOptions = new();
            socketOptions.SetConnectTimeoutMillis(_options.SocketConnectTimeout.ToMillisInt32());
            clusterBuilder.WithSocketOptions(socketOptions);
        }
        if (_options.ExponentialReconnectPolicy)
        {
            clusterBuilder.WithReconnectionPolicy(new ExponentialReconnectionPolicy(
                baseDelayMs: _options.ExponentialReconnectPolicyBaseDelay.ToMillisInt32(),
                maxDelayMs: _options.ExponentialReconnectPolicyMaxDelay.ToMillisInt32()));
        }

        Cluster cluster = clusterBuilder.Build();
        return cluster;
    }

    public void Dispose()
    {
        Dispose(isDisposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            _cluster.Value.Dispose();
        }
    }

    private Cluster Cluster => _cluster.Value;

    public ISession GetSession()
    {
        const string sessionWithoutKeyspace = "";
        ISession session = _sessions.GetOrAdd(sessionWithoutKeyspace, _ => Cluster.Connect());
        return session;
    }

    public ISession GetSession(string keyspace)
    {
        ValidateKeyspace(keyspace);
        ISession session = _sessions.GetOrAdd(keyspace, k => Cluster.Connect(k));
        return session;
    }

    public bool ExistsKeyspace(string keyspace)
    {
        ValidateKeyspace(keyspace);
        // force connection create
        GetSession();
        KeyspaceMetadata metadata = Cluster.Metadata.GetKeyspace(keyspace);
        bool exists = metadata != null;
        return exists;
    }

    // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
    private static void ValidateKeyspace(string keyspace)
    {
        if (string.IsNullOrWhiteSpace(keyspace))
            throw new ArgumentException($"Parameter '{nameof(keyspace)}' should not be null or whitespace.");
    }
}