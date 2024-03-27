using CecoChat.User.Contracts;
using Common.Redis;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CecoChat.Data.User.Entities.Profiles;

public class CachingProfileQueryRepo : IProfileQueryRepo
{
    private readonly ILogger _logger;
    private readonly UserCacheOptions _cacheOptions;
    private readonly IDatabase _cache;
    private readonly IProfileCache _profileCache;
    private readonly IProfileQueryRepo _decoratedRepo;

    public CachingProfileQueryRepo(
        ILogger<CachingProfileQueryRepo> logger,
        IOptions<UserCacheOptions> cacheOptions,
        IRedisContext redisContext,
        IProfileCache profileCache,
        IProfileQueryRepo decoratedRepo)
    {
        _logger = logger;
        _cacheOptions = cacheOptions.Value;
        _cache = redisContext.GetDatabase();
        _profileCache = profileCache;
        _decoratedRepo = decoratedRepo;
    }

    public Task<FullProfileResult> GetFullProfile(string userName, bool includePassword)
    {
        // no caching here
        return _decoratedRepo.GetFullProfile(userName, includePassword);
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
        RedisKey profileKey = _profileCache.CreateKey(requestedUserId);
        ProfilePublic? profile = await _profileCache.GetOne(profileKey);

        if (profile == null)
        {
            profile = await _decoratedRepo.GetPublicProfile(requestedUserId, userId);
            if (profile != null)
            {
                await _profileCache.SetOneAsynchronously(profile);
            }

            return (profile, "DB");
        }
        else
        {
            return (profile, "cache");
        }
    }

    public async Task<IReadOnlyCollection<ProfilePublic>> GetPublicProfiles(IList<long> requestedUserIds, long userId)
    {
        if (!_cacheOptions.Enabled)
        {
            IReadOnlyCollection<ProfilePublic> profiles = await _decoratedRepo.GetPublicProfiles(requestedUserIds, userId);
            LogFetchedPublicProfiles(totalCount: requestedUserIds.Count, fromCacheCount: 0, fromDbCount: requestedUserIds.Count, userId);
            return profiles;
        }

        RedisKey[] profileKeys = requestedUserIds.Select(_profileCache.CreateKey).ToArray();
        RedisValue[] cachedProfiles = await _profileCache.GetMany(profileKeys);
        List<ProfilePublic> resultProfiles = new(capacity: requestedUserIds.Count);

        List<long>? uncachedUserIds = ProcessProfilesFromCache(requestedUserIds, cachedProfiles, resultProfiles);
        int profilesFromCacheCount = resultProfiles.Count;
        if (uncachedUserIds != null && uncachedUserIds.Count > 0)
        {
            await LoadUncachedProfiles(uncachedUserIds, userId, resultProfiles);
        }

        LogFetchedPublicProfiles(totalCount: requestedUserIds.Count, fromCacheCount: profilesFromCacheCount, fromDbCount: uncachedUserIds?.Count ?? 0, userId);
        return resultProfiles;
    }

    private void LogFetchedPublicProfiles(int totalCount, int fromCacheCount, int fromDbCount, long userId)
    {
        _logger.LogTrace(
            "Fetched {PublicProfileCount} public profiles in total ({FromCacheCount} from cache, {FromDbCount} from DB) in the ID list as requested by user {UserId}",
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

    private async Task LoadUncachedProfiles(List<long> uncachedUserIds, long userId, ICollection<ProfilePublic> output)
    {
        IEnumerable<ProfilePublic> uncachedProfiles = await _decoratedRepo.GetPublicProfiles(uncachedUserIds, userId);

        foreach (ProfilePublic uncachedProfile in uncachedProfiles)
        {
            output.Add(uncachedProfile);
            await _profileCache.SetOneAsynchronously(uncachedProfile);
        }
    }

    public async Task<IReadOnlyCollection<ProfilePublic>> GetPublicProfiles(string searchPattern, int profileCount, long userId)
    {
        if (!_cacheOptions.Enabled)
        {
            IReadOnlyCollection<ProfilePublic> profiles = await _decoratedRepo.GetPublicProfiles(searchPattern, profileCount, userId);
            LogProfileSearch(dataSourceAndAction: "DB", profiles.Count, searchPattern, userId);
            return profiles;
        }

        RedisKey key = $"profile-search:{searchPattern}";
        RedisValue value = await _cache.StringGetAsync(key);
        IReadOnlyCollection<ProfilePublic> cachedProfiles;

        if (value.IsNullOrEmpty)
        {
            cachedProfiles = await _decoratedRepo.GetPublicProfiles(searchPattern, profileCount, userId);
            ProfileSearchResult searchResult = new();
            searchResult.Profiles.Add(cachedProfiles);

            byte[] searchResultBytes = searchResult.ToByteArray();
            await _cache.StringSetAsync(key, searchResultBytes, expiry: _cacheOptions.ProfileSearchDuration);
            LogProfileSearch(dataSourceAndAction: "DB and then cached", cachedProfiles.Count, searchPattern, userId);
        }
        else
        {
            ProfileSearchResult searchResult = ProfileSearchResult.Parser.ParseFrom(value);
            cachedProfiles = searchResult.Profiles;
            LogProfileSearch(dataSourceAndAction: "cache", cachedProfiles.Count, searchPattern, userId);
        }

        return cachedProfiles;
    }

    private void LogProfileSearch(string dataSourceAndAction, int profileCount, string searchPattern, long userId)
    {
        _logger.LogTrace(
            "Fetched from {DataSourceAndAction} {PublicProfileCount} public profiles matching the search pattern {ProfileSearchPattern} as requested by user {UserId}",
            dataSourceAndAction, profileCount, searchPattern, userId);
    }
}
