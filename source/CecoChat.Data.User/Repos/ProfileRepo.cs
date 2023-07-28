using System.Globalization;
using AutoMapper;
using CecoChat.Contracts.User;
using CecoChat.Redis;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CecoChat.Data.User.Repos;

public interface IProfileRepo
{
    Task<ProfileFull?> GetFullProfile(long requestedUserId);

    Task<ProfilePublic?> GetPublicProfile(long requestedUserId, long userId);

    Task<IEnumerable<ProfilePublic>> GetPublicProfiles(IList<long> requestedUserIds, long userId);
}

internal class ProfileRepo : IProfileRepo
{
    private readonly ILogger _logger;
    private readonly UserDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IRedisContext _redisContext;

    public ProfileRepo(ILogger<ProfileRepo> logger, UserDbContext dbContext, IMapper mapper, IRedisContext redisContext)
    {
        _logger = logger;
        _dbContext = dbContext;
        _mapper = mapper;
        _redisContext = redisContext;
    }

    public async Task<ProfileFull?> GetFullProfile(long requestedUserId)
    {
        ProfileEntity? entity = await _dbContext.Profiles.FirstOrDefaultAsync(profile => profile.UserId == requestedUserId);
        if (entity == null)
        {
            _logger.LogTrace("Failed to fetch full profile for user {UserId}", requestedUserId);
            return null;
        }

        _logger.LogTrace("Fetched full profile {ProfileUserName} for user {UserId}", entity.UserName, entity.UserId);

        ProfileFull profile = _mapper.Map<ProfileFull>(entity);
        return profile;
    }

    public async Task<ProfilePublic?> GetPublicProfile(long requestedUserId, long userId)
    {
        IDatabase cache = _redisContext.GetDatabase();
        RedisKey profileKey = CreateUserProfileKey(requestedUserId);
        RedisValue cachedProfile = await cache.StringGetAsync(profileKey);
        ProfilePublic? profile;
        string profileSource;

        if (cachedProfile.IsNullOrEmpty)
        {
            ProfileEntity? entity = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == requestedUserId);
            if (entity == null)
            {
                _logger.LogTrace("Failed to fetch public profile for user {UserId}", requestedUserId);
                return null;
            }

            profile = _mapper.Map<ProfilePublic>(entity);
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
        IDatabase cache = _redisContext.GetDatabase();
        RedisKey[] profileKeys = requestedUserIds.Select(CreateUserProfileKey).ToArray();
        RedisValue[] cachedProfiles = await cache.StringGetAsync(profileKeys);
        List<ProfilePublic> resultProfiles = new(capacity: requestedUserIds.Count);

        List<long>? uncachedUserIds = ProcessProfilesFromCache(requestedUserIds, cachedProfiles, resultProfiles);
        int profilesFromCacheCount = resultProfiles.Count;
        if (uncachedUserIds != null && uncachedUserIds.Count > 0)
        {
            await LoadUncachedProfiles(uncachedUserIds, resultProfiles, cache);
        }

        _logger.LogTrace(
            "Fetched {PublicProfileCount} public profiles in total ({FromCacheCount} from cache, {FromDbCount} from DB) as requested by user {UserId}",
            requestedUserIds.Count, profilesFromCacheCount, uncachedUserIds?.Count ?? 0, userId);
        return resultProfiles;
    }

    private List<long>? ProcessProfilesFromCache(IList<long> userIds, RedisValue[] cachedProfiles, ICollection<ProfilePublic> output)
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

    private async Task LoadUncachedProfiles(List<long> uncachedUserIds, ICollection<ProfilePublic> output, IDatabase cache)
    {
        IEnumerable<ProfilePublic> uncachedProfiles = await _dbContext.Profiles
            .Where(entity => uncachedUserIds.Contains(entity.UserId))
            .Select(entity => _mapper.Map<ProfilePublic>(entity))
            .ToListAsync();

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
