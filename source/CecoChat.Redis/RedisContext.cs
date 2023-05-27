using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CecoChat.Redis;

public interface IRedisContext : IDisposable, IAsyncDisposable
{
    IConnectionMultiplexer Connection { get; }

    IDatabase GetDatabase(int db = -1);

    ISubscriber GetSubscriber();
}

public sealed class RedisContext : IRedisContext
{
    private readonly ILogger _logger;
    private readonly RedisOptions _redisOptions;
    private readonly Lazy<ConnectionMultiplexer> _connection;

    public RedisContext(
        ILogger<RedisContext> logger,
        IOptions<RedisOptions> options)
    {
        _logger = logger;
        _redisOptions = options.Value;
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

    public IDatabase GetDatabase(int db = -1)
    {
        return Connection.GetDatabase(db);
    }

    public ISubscriber GetSubscriber()
    {
        return Connection.GetSubscriber();
    }

    private ConnectionMultiplexer Connect()
    {
        ConfigurationOptions redisConfiguration = CreateRedisConfiguration();
        ConnectionMultiplexer connection = ConnectionMultiplexer.Connect(redisConfiguration);
        _logger.LogInformation("Redis connection '{ConnectionString}' is {ConnectionState}",
            connection.Configuration, connection.IsConnected ? "established" : "not established");

        connection.ConnectionFailed += (_, args) =>
        {
            _logger.LogWarning(args.Exception, "Redis connection to {$Endpoint} failed", args.EndPoint);
        };
        connection.ConnectionRestored += (_, args) =>
        {
            _logger.LogInformation("Redis connection to {$Endpoint} restored", args.EndPoint);
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
