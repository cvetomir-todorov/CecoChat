using System.Globalization;
using CecoChat.Contracts.User;
using CecoChat.Redis;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CecoChat.Data.User.Repos;

public class CachingProfileRepo : IProfileRepo
{
    private readonly ILogger _logger;
    private readonly UserCacheOptions _cacheOptions;
    private readonly IRedisContext _cacheContext;
    private readonly IProfileRepo _decoratedRepo;

    public CachingProfileRepo(
        ILogger<CachingProfileRepo> logger,
        IOptions<UserCacheOptions> cacheOptions,
        IRedisContext cacheContext,
        IProfileRepo decoratedRepo)
    {
        _logger = logger;
        _cacheOptions = cacheOptions.Value;
        _cacheContext = cacheContext;
        _decoratedRepo = decoratedRepo;
    }

    public Task<ProfileFull?> GetFullProfile(long requestedUserId)
    {
        // no caching here
        return _decoratedRepo.GetFullProfile(requestedUserId);
    }

    public async Task<ProfilePublic?> GetPublicProfile(long requestedUserId, long userId)
    {
        if (!_cacheOptions.Enabled)
        {
            return await _decoratedRepo.GetPublicProfile(requestedUserId, userId);
        }

        IDatabase cache = _cacheContext.GetDatabase();
        RedisKey profileKey = CreateUserProfileKey(requestedUserId);
        RedisValue cachedProfile = await cache.StringGetAsync(profileKey);
        ProfilePublic? profile;
        string profileSource;

        if (cachedProfile.IsNullOrEmpty)
        {
            profile = await _decoratedRepo.GetPublicProfile(requestedUserId, userId);
            if (profile == null)
            {
                _logger.LogTrace("Failed to fetch public profile for user {UserId}", requestedUserId);
                return null;
            }

            profileSource = "DB";
            byte[] profileBytes = profile.ToByteArray();
            await cache.StringSetAsync(profileKey, profileBytes);
        }
        else
        {
            profile = ProfilePublic.Parser.ParseFrom(cachedProfile);
            profileSource = "cache";
        }

        _logger.LogTrace("Fetched from {ProfileSource} public profile for user {RequestedUserId} requested by user {UserId}", profileSource, requestedUserId, userId);
        return profile;
    }

    public async Task<IEnumerable<ProfilePublic>> GetPublicProfiles(IList<long> requestedUserIds, long userId)
    {
        if (!_cacheOptions.Enabled)
        {
            return await _decoratedRepo.GetPublicProfiles(requestedUserIds, userId);
        }

        IDatabase cache = _cacheContext.GetDatabase();
        RedisKey[] profileKeys = requestedUserIds.Select(CreateUserProfileKey).ToArray();
        RedisValue[] cachedProfiles = await cache.StringGetAsync(profileKeys);
        List<ProfilePublic> resultProfiles = new(capacity: requestedUserIds.Count);

        List<long>? uncachedUserIds = ProcessProfilesFromCache(requestedUserIds, cachedProfiles, resultProfiles);
        int profilesFromCacheCount = resultProfiles.Count;
        if (uncachedUserIds != null && uncachedUserIds.Count > 0)
        {
            await LoadUncachedProfiles(uncachedUserIds, userId, resultProfiles, cache);
        }

        _logger.LogTrace(
            "Fetched {PublicProfileCount} public profiles in total ({FromCacheCount} from cache, {FromDbCount} from DB) as requested by user {UserId}",
            requestedUserIds.Count, profilesFromCacheCount, uncachedUserIds?.Count ?? 0, userId);
        return resultProfiles;
    }

    private static List<long>? ProcessProfilesFromCache(IList<long> userIds, RedisValue[] cachedProfiles, ICollection<ProfilePublic> output)
    {
        List<long>? uncachedUserIds = null;

        for (int i = 0; i < userIds.Count; ++i)
        {
            RedisValue cachedProfile = cachedProfiles[i];

            if (cachedProfile.IsNullOrEmpty)
            {
                uncachedUserIds ??= new List<long>();
                uncachedUserIds.Add(userIds[i]);
            }
            else
            {
                ProfilePublic profile = ProfilePublic.Parser.ParseFrom(cachedProfile);
                output.Add(profile);
            }
        }

        return uncachedUserIds;
    }

    private async Task LoadUncachedProfiles(List<long> uncachedUserIds, long userId, ICollection<ProfilePublic> output, IDatabase cache)
    {
        IEnumerable<ProfilePublic> uncachedProfiles = await _decoratedRepo.GetPublicProfiles(uncachedUserIds, userId);

        KeyValuePair<RedisKey, RedisValue>[] toAddToCache = new KeyValuePair<RedisKey, RedisValue>[uncachedUserIds.Count];
        int index = 0;

        foreach (ProfilePublic uncachedProfile in uncachedProfiles)
        {
            output.Add(uncachedProfile);

            toAddToCache[index] = new(
                key: CreateUserProfileKey(uncachedProfile.UserId),
                value: uncachedProfile.ToByteArray());

            index++;
        }

        await cache.StringSetAsync(toAddToCache);
    }

    private static RedisKey CreateUserProfileKey(long userId)
    {
        // we store all profiles in a separate database, so we don't need to prefix the key
        return new RedisKey(userId.ToString(CultureInfo.InvariantCulture));
    }
}
