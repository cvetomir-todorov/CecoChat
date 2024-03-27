using CecoChat.User.Contracts;
using Common.Redis;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CecoChat.User.Data.Entities.Connections;

public class CachingConnectionQueryRepo : ICachingConnectionQueryRepo
{
    private readonly ILogger _logger;
    private readonly UserCacheOptions _cacheOptions;
    private readonly IDatabase _cache;
    private readonly IConnectionQueryRepo _decoratedRepo;

    public CachingConnectionQueryRepo(
        ILogger<CachingConnectionQueryRepo> logger,
        IOptions<UserCacheOptions> userCacheOptions,
        IRedisContext redisContext,
        IConnectionQueryRepo decoratedRepo)
    {
        _logger = logger;
        _cacheOptions = userCacheOptions.Value;
        _cache = redisContext.GetDatabase();
        _decoratedRepo = decoratedRepo;
    }

    public async Task<Connection?> GetConnection(long userId, long connectionId)
    {
        // use hash tags to make sure the hash slot is determined only by the user ID
        RedisKey key = new($"connections:{{{userId}}}:{connectionId}");
        RedisValue value = await _cache.StringGetAsync(key);
        Connection? connection;

        if (value.IsNullOrEmpty)
        {
            connection = await _decoratedRepo.GetConnection(userId, connectionId);
            if (connection != null)
            {
                byte[] connectionBytes = connection.ToByteArray();
                await _cache.StringSetAsync(key, connectionBytes, expiry: _cacheOptions.ConnectionEntriesDuration);

                _logger.LogTrace("Fetched from DB and then cached for user {UserId} a connection to {ConnectionId}", userId, connectionId);
            }
        }
        else
        {
            byte[] connectionBytes = value!;
            connection = Connection.Parser.ParseFrom(connectionBytes);
            _logger.LogTrace("Fetched from cache for user {UserId} a connection to {ConnectionId}", userId, connectionId);
        }

        return connection;
    }

    public async Task<IReadOnlyCollection<Connection>> GetConnections(long userId)
    {
        // use hash tags to make sure the hash slot is determined only by the user ID
        RedisKey key = new($"connections:{{{userId}}}");
        RedisValue[] existingValues = await _cache.SetMembersAsync(key);
        IReadOnlyCollection<Connection> connections;

        if (existingValues.Length == 0)
        {
            connections = await _decoratedRepo.GetConnections(userId);
            if (connections.Count > 0)
            {
                RedisValue[] newValues = new RedisValue[connections.Count];
                int index = 0;

                foreach (Connection connection in connections)
                {
                    byte[] connectionBytes = connection.ToByteArray();
                    newValues[index] = connectionBytes;
                    index++;
                }

                await _cache.SetAddAsync(key, newValues);
                await _cache.KeyExpireAsync(key, _cacheOptions.ConnectionEntriesDuration);

                _logger.LogTrace("Fetched from DB and then cached for user {UserId} {ConnectionCount} connections", userId, connections.Count);
            }
        }
        else
        {
            List<Connection> connectionList = new(capacity: existingValues.Length);

            foreach (RedisValue existingValue in existingValues)
            {
                byte[] connectionBytes = existingValue!;
                Connection connection = Connection.Parser.ParseFrom(connectionBytes);
                connectionList.Add(connection);
            }

            connections = connectionList;
            _logger.LogTrace("Fetched from cache for user {UserId} {ConnectionCount} connections", userId, connections.Count);
        }

        return connections;
    }
}
