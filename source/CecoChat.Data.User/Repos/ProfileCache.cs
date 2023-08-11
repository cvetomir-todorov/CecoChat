using System.Globalization;
using System.Threading.Channels;
using CecoChat.Contracts.User;
using CecoChat.Redis;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CecoChat.Data.User.Repos;

/// <summary>
/// The abstraction leaks Redis details, but that avoids allocations, so it's acceptable.
/// </summary>
public interface IProfileCache
{
    RedisKey CreateKey(long userId);

    Task<ProfilePublic?> GetOne(RedisKey key);

    Task<RedisValue[]> GetMany(RedisKey[] keys);

    ValueTask SetOneAsynchronously(ProfilePublic profile);

    void StartProcessing(CancellationToken ct);
}

internal class ProfileCache : IProfileCache
{
    private readonly ILogger _logger;
    private readonly UserCacheOptions _cacheOptions;
    private readonly IDatabase _cache;
    private readonly Channel<ProfilePublic> _channel;

    public ProfileCache(
        ILogger<ProfileCache> logger,
        IOptions<UserCacheOptions> userCacheOptions,
        IRedisContext redisContext)
    {
        _logger = logger;
        _cacheOptions = userCacheOptions.Value;
        _cache = redisContext.GetDatabase();
        _channel = Channel.CreateUnbounded<ProfilePublic>(new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = false
        });
    }

    public RedisKey CreateKey(long userId)
    {
        // we store all profiles in a separate database, so we don't need to prefix the key
        return new RedisKey(userId.ToString(CultureInfo.InvariantCulture));
    }

    public async Task<ProfilePublic?> GetOne(RedisKey key)
    {
        RedisValue entry = await _cache.StringGetAsync(key);
        if (entry.IsNullOrEmpty)
        {
            return null;
        }

        ProfilePublic profile = ProfilePublic.Parser.ParseFrom(entry);
        return profile;
    }

    public Task<RedisValue[]> GetMany(RedisKey[] keys)
    {
        return _cache.StringGetAsync(keys);
    }

    public ValueTask SetOneAsynchronously(ProfilePublic profile)
    {
        return _channel.Writer.WriteAsync(profile);
    }

    public void StartProcessing(CancellationToken ct)
    {
        int processors = _cacheOptions.AsyncProfileProcessors;
        if (processors <= 0)
        {
            throw new InvalidOperationException("Async profile cache processors should be > 0.");
        }

        for (int i = 0; i < processors; ++i)
        {
            Task.Run(async () =>
            {
                try
                {
                    await ProcessChannel(ct);
                }
                catch (Exception exception)
                {
                    _logger.LogCritical(exception, "Async profile cache processor failed");
                }
            }, ct);
        }

        _logger.LogInformation("Started async profile caching with {AsyncProfileCacheProcessorCount} processors", processors);
    }

    private async Task ProcessChannel(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            ProfilePublic profile = await _channel.Reader.ReadAsync(ct);
            byte[] profileBytes = profile.ToByteArray();
            RedisKey key = CreateKey(profile.UserId);

            _cache.StringSet(key, profileBytes, _cacheOptions.ProfileEntryDuration);
            _logger.LogTrace("Cached profile for user {UserId}", profile.UserId);
        }
    }
}
