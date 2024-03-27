using System.Threading.Channels;
using CecoChat.User.Contracts;
using Common.Redis;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CecoChat.User.Data.Entities.Profiles;

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
        return new RedisKey($"profiles:{userId}");
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

    public async Task<RedisValue[]> GetMany(RedisKey[] keys)
    {
        // keys may be spread throughout many partitions and we need the values for all of them
        // we can pipeline a request for each key individually which is wasteful
        // instead we pipeline a request for our keys in each hash slot which is more optimal

        IEnumerable<IGrouping<int, RedisKey>> keyGroups = keys.GroupBy(key => _cache.Multiplexer.GetHashSlot(key));

        Task<RedisValue[]>[] tasks = keyGroups.Select(async keyGroup =>
            {
                _logger.LogTrace("Getting profiles from Redis hash slot {RedisHashSlot}", keyGroup.Key);
                RedisKey[] keyGroupArray = keyGroup.ToArray();
                RedisValue[] valuesGroup = await _cache.StringGetAsync(keyGroupArray);

                return valuesGroup;
            })
            .ToArray();

        await Task.WhenAll(tasks);
        RedisValue[] values = new RedisValue[keys.Length];
        int index = 0;

        foreach (Task<RedisValue[]> task in tasks)
        {
            RedisValue[] valuesGroup = task.Result;
            valuesGroup.CopyTo(values, index);
            index += valuesGroup.Length;
        }

        return values;
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
            int processorId = i;
            Task.Run(async () =>
            {
                try
                {
                    await ProcessChannel(ct);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Async profile cache processor {Processor} was cancelled", processorId);
                }
                catch (Exception exception)
                {
                    _logger.LogCritical(exception, "Async profile cache processor {Processor} failed", processorId);
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
