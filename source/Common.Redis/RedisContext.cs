using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Common.Redis;

public interface IRedisContext : IDisposable, IAsyncDisposable
{
    IConnectionMultiplexer Connection { get; }

    IDatabase GetDatabase();

    ISubscriber GetSubscriber();
}

public sealed class RedisContext : IRedisContext
{
    private readonly ILogger _logger;
    private readonly RedisOptions _redisOptions;
    private readonly Lazy<ConnectionMultiplexer> _connection;

    public RedisContext(
        ILogger<RedisContext> logger,
        RedisOptions options)
    {
        _logger = logger;
        _redisOptions = options;
        _connection = new(Connect);
    }

    public void Dispose()
    {
        if (_connection.IsValueCreated)
        {
            _connection.Value.Close();
        }
    }

    public ValueTask DisposeAsync()
    {
        if (_connection.IsValueCreated)
        {
            return new ValueTask(_connection.Value.CloseAsync());
        }

        return ValueTask.CompletedTask;
    }

    public IConnectionMultiplexer Connection => _connection.Value;

    public IDatabase GetDatabase()
    {
        return Connection.GetDatabase();
    }

    public ISubscriber GetSubscriber()
    {
        return Connection.GetSubscriber();
    }

    private ConnectionMultiplexer Connect()
    {
        ConfigurationOptions redisConfiguration = CreateRedisConfiguration();
        ConnectionMultiplexer connection = ConnectionMultiplexer.Connect(redisConfiguration);
        _logger.LogInformation("Redis connection {Name} using '{ConnectionString}' is {ConnectionState}",
            _redisOptions.Name, connection.Configuration, connection.IsConnected ? "established" : "not established");

        connection.ConnectionFailed += (_, args) =>
        {
            _logger.LogWarning(args.Exception, "Redis connection {Name} to {Endpoint} failed", _redisOptions.Name, args.EndPoint);
        };
        connection.ConnectionRestored += (_, args) =>
        {
            _logger.LogInformation("Redis connection {Name} to {Endpoint} restored", _redisOptions.Name, args.EndPoint);
        };

        return connection;
    }

    private ConfigurationOptions CreateRedisConfiguration()
    {
        ConfigurationOptions redisConfiguration = new()
        {
            ConnectRetry = _redisOptions.ConnectRetry,
            ConnectTimeout = _redisOptions.ConnectTimeout,
            KeepAlive = _redisOptions.KeepAlive,
            ReconnectRetryPolicy = new ExponentialRetry(deltaBackOffMilliseconds: 1000)
        };

        foreach (string endpoint in _redisOptions.Endpoints)
        {
            redisConfiguration.EndPoints.Add(endpoint);
        }

        return redisConfiguration;
    }
}
