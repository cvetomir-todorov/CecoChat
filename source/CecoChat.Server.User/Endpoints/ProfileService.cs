using System.Globalization;
using CecoChat.Contracts.User;
using CecoChat.Data.User.Repos;
using CecoChat.Redis;
using CecoChat.Server.Identity;
using Google.Protobuf;
using Grpc.Core;
using StackExchange.Redis;

namespace CecoChat.Server.User.Endpoints;

public class ProfileService : Profile.ProfileBase
{
    private readonly ILogger _logger;
    private readonly IProfileRepo _repo;
    private readonly IRedisContext _redisContext;
    private const int RedisDbIndex = 1;

    public ProfileService(ILogger<ProfileService> logger, IProfileRepo repo, IRedisContext redisContext)
    {
        _logger = logger;
        _repo = repo;
        _redisContext = redisContext;
    }

    public override async Task<GetFullProfileResponse> GetFullProfile(GetFullProfileRequest request, ServerCallContext context)
    {
        if (!context.GetHttpContext().TryGetUserClaims(_logger, out UserClaims? userClaims))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, string.Empty));
        }

        ProfileFull? profile = await _repo.GetFullProfile(userClaims.UserId);
        if (profile == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"There is no profile for user ID {userClaims.UserId}."));
        }

        _logger.LogTrace("Responding with full profile {ProfileUserName} for user {UserId}", profile.UserName, userClaims.UserId);
        return new GetFullProfileResponse { Profile = profile };
    }

    public override async Task<GetPublicProfileResponse> GetPublicProfile(GetPublicProfileRequest request, ServerCallContext context)
    {
        if (!context.GetHttpContext().TryGetUserClaims(_logger, out UserClaims? userClaims))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, string.Empty));
        }

        IDatabase cache = _redisContext.GetDatabase(RedisDbIndex);
        RedisKey profileKey = CreateUserProfileKey(request.UserId);
        RedisValue cachedProfile = await cache.StringGetAsync(profileKey);
        ProfilePublic? profile;

        if (cachedProfile.IsNullOrEmpty)
        {
            profile = await _repo.GetPublicProfile(request.UserId, userClaims.UserId);
            if (profile == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"There is no profile for user ID {request.UserId}."));
            }

            byte[] profileBytes = profile.ToByteArray();
            await cache.StringSetAsync(profileKey, profileBytes);
        }
        else
        {
            profile = ProfilePublic.Parser.ParseFrom(cachedProfile);
        }

        _logger.LogTrace("Responding with public profile {RequestedUserId} requested by user {UserId}", profile.UserId, userClaims.UserId);
        return new GetPublicProfileResponse { Profile = profile };
    }

    public override async Task<GetPublicProfilesResponse> GetPublicProfiles(GetPublicProfilesRequest request, ServerCallContext context)
    {
        if (!context.GetHttpContext().TryGetUserClaims(_logger, out UserClaims? userClaims))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, string.Empty));
        }

        IDatabase cache = _redisContext.GetDatabase(RedisDbIndex);
        RedisKey[] profileKeys = request.UserIds.Select(CreateUserProfileKey).ToArray();
        RedisValue[] cachedProfiles = await cache.StringGetAsync(profileKeys);
        List<long>? missingUserIds = null;
        GetPublicProfilesResponse response = new();

        for (int i = 0; i < request.UserIds.Count; ++i)
        {
            RedisValue cachedProfile = cachedProfiles[i];

            if (cachedProfile.IsNullOrEmpty)
            {
                missingUserIds ??= new List<long>();
                missingUserIds.Add(request.UserIds[i]);
            }
            else
            {
                ProfilePublic profile = ProfilePublic.Parser.ParseFrom(cachedProfile);
                response.Profiles.Add(profile);
            }
        }

        if (missingUserIds != null && missingUserIds.Count > 0)
        {
            IEnumerable<ProfilePublic> missingProfiles = await _repo.GetPublicProfiles(missingUserIds, userClaims.UserId);
            KeyValuePair<RedisKey, RedisValue>[] toAddToCache = new KeyValuePair<RedisKey, RedisValue>[missingUserIds.Count];
            int index = 0;

            foreach (ProfilePublic missingProfile in missingProfiles)
            {
                response.Profiles.Add(missingProfile);

                toAddToCache[index] = new KeyValuePair<RedisKey, RedisValue>(
                    key: CreateUserProfileKey(missingProfile.UserId),
                    value: missingProfile.ToByteArray());

                index++;
            }

            await cache.StringSetAsync(toAddToCache);
        }

        _logger.LogTrace("Responding with {PublicProfileCount} public profiles requested by user {UserId}", response.Profiles.Count, userClaims.UserId);
        return response;
    }

    private static RedisKey CreateUserProfileKey(long userId)
    {
        // we store all profiles in a separate database, so we don't need to prefix the key
        return new RedisKey(userId.ToString(CultureInfo.InvariantCulture));
    }
}
