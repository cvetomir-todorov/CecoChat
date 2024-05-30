using System.Collections.Concurrent;
using System.Net;
using Cassandra;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Common.Cassandra;

public interface ICassandraDbContext : IDisposable
{
    ISession GetSession();

    ISession GetSession(string keyspace);

    bool ExistsKeyspace(string keyspace);
}

public class CassandraDbContext : ICassandraDbContext
{
    private readonly ILogger _logger;
    private readonly CassandraOptions _options;
    private readonly Lazy<Cluster> _cluster;
    private readonly ConcurrentDictionary<string, ISession> _sessions;

    public CassandraDbContext(ILogger<CassandraDbContext> logger, IOptions<CassandraOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _cluster = new Lazy<Cluster>(CreateCluster);
        _sessions = new ConcurrentDictionary<string, ISession>();
    }

    private Cluster CreateCluster()
    {
        List<IPEndPoint> endpoints = GetValidEndpoints();

        Builder clusterBuilder = new Builder()
            .AddContactPoints(endpoints)
            .WithCompression(CompressionType.LZ4)
            .WithLoadBalancingPolicy(new DCAwareRoundRobinPolicy(_options.LocalDc));

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

    private List<IPEndPoint> GetValidEndpoints()
    {
        List<string> errors = new();
        HashSet<string> uniqueEndpoints = new();
        List<IPEndPoint> endpoints = new(capacity: _options.ContactPoints.Length);

        foreach (string contactPoint in _options.ContactPoints)
        {
            if (!uniqueEndpoints.Add(contactPoint))
            {
                errors.Add($"contact point '{contactPoint}' is duplicate");
                continue;
            }

            string[] parts = contactPoint.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0 || !int.TryParse(parts[1], out int hostPort))
            {
                errors.Add($"contact point '{contactPoint}' should be in format 'host:port'");
            }
            else
            {
                if (!IPAddress.TryParse(parts[0], out IPAddress? hostIp))
                {
                    hostIp = Dns.GetHostEntry(parts[0]).AddressList[0];
                }

                endpoints.Add(new IPEndPoint(hostIp, hostPort));
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Cassandra errors: {string.Join(", ", errors)}");
        }

        _logger.LogInformation("Use Cassandra nodes {CassandraNodes}", string.Join(", ", uniqueEndpoints));
        return endpoints;
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
