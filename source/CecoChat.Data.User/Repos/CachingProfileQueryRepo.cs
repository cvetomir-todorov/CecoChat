using System.Globalization;
using CecoChat.Contracts.User;
using CecoChat.Redis;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CecoChat.Data.User.Repos;

public class CachingProfileQueryRepo : IProfileQueryRepo
{
    private readonly ILogger _logger;
    private readonly UserCacheOptions _cacheOptions;
    private readonly IRedisContext _cacheContext;
    private readonly IProfileQueryRepo _decoratedRepo;

    public CachingProfileQueryRepo(
        ILogger<CachingProfileQueryRepo> logger,
        IOptions<UserCacheOptions> cacheOptions,
        IRedisContext cacheContext,
        IProfileQueryRepo decoratedRepo)
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
        ProfilePublic? profile;
        string profileSource;

        if (!_cacheOptions.Enabled)
        {
            profile = await _decoratedRepo.GetPublicProfile(requestedUserId, userId);
            profileSource = "DB";
        }
        else
        {
            (profile, profileSource) = await GetPublicProfileUsingCache(requestedUserId, userId);
        }

        if (profile == null)
        {
            _logger.LogTrace("Failed to fetch from {ProfileSource} public profile for user {UserId}", profileSource, requestedUserId);
        }
        else
        {
            _logger.LogTrace("Fetched from {ProfileSource} public profile for user {RequestedUserId} requested by user {UserId}", profileSource, requestedUserId, userId);
        }

        return profile;
    }

    private async Task<(ProfilePublic?, string)> GetPublicProfileUsingCache(long requestedUserId, long userId)
    {
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
                return (null, "DB");
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

        return (profile, profileSource);
    }

    public async Task<IEnumerable<ProfilePublic>> GetPublicProfiles(IList<long> requestedUserIds, long userId)
    {
        if (!_cacheOptions.Enabled)
        {
            IEnumerable<ProfilePublic> profiles = await _decoratedRepo.GetPublicProfiles(requestedUserIds, userId);
            LogFetchedPublicProfiles(totalCount: requestedUserIds.Count, fromCacheCount: 0, fromDbCount: requestedUserIds.Count, userId);
            return profiles;
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

        LogFetchedPublicProfiles(totalCount: requestedUserIds.Count, fromCacheCount: profilesFromCacheCount, fromDbCount: uncachedUserIds?.Count ?? 0, userId);
        return resultProfiles;
    }

    private void LogFetchedPublicProfiles(int totalCount, int fromCacheCount, int fromDbCount, long userId)
    {
        _logger.LogTrace(
            "Fetched {PublicProfileCount} public profiles in total ({FromCacheCount} from cache, {FromDbCount} from DB) as requested by user {UserId}",
            totalCount, fromCacheCount, fromDbCount, userId);
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
