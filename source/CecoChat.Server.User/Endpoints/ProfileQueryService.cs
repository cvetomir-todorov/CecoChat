using CecoChat.Contracts.User;
using CecoChat.Data.User.Repos;
using CecoChat.Server.Identity;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace CecoChat.Server.User.Endpoints;

public class ProfileQueryService : ProfileQuery.ProfileQueryBase
{
    private readonly ILogger _logger;
    private readonly IProfileQueryRepo _repo;

    public ProfileQueryService(ILogger<ProfileQueryService> logger, IProfileQueryRepo repo)
    {
        _logger = logger;
        _repo = repo;
    }

    [Authorize(Roles = "user")]
    public override async Task<GetFullProfileResponse> GetFullProfile(GetFullProfileRequest request, ServerCallContext context)
    {
        UserClaims userClaims = GetUserClaims(context);

        ProfileFull? profile = await _repo.GetFullProfile(userClaims.UserId);
        if (profile == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"There is no profile for user ID {userClaims.UserId}."));
        }

        _logger.LogTrace("Responding with full profile {ProfileUserName} for user {UserId}", profile.UserName, userClaims.UserId);
        return new GetFullProfileResponse { Profile = profile };
    }

    [Authorize(Roles = "user")]
    public override async Task<GetPublicProfileResponse> GetPublicProfile(GetPublicProfileRequest request, ServerCallContext context)
    {
        UserClaims userClaims = GetUserClaims(context);

        ProfilePublic? profile = await _repo.GetPublicProfile(request.UserId, userClaims.UserId);
        if (profile == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"There is no profile for user ID {request.UserId}."));
        }

        _logger.LogTrace("Responding with public profile {RequestedUserId} requested by user {UserId}", profile.UserId, userClaims.UserId);
        return new GetPublicProfileResponse { Profile = profile };
    }

    [Authorize(Roles = "user")]
    public override async Task<GetPublicProfilesResponse> GetPublicProfiles(GetPublicProfilesRequest request, ServerCallContext context)
    {
        UserClaims userClaims = GetUserClaims(context);

        IEnumerable<ProfilePublic> profiles = await _repo.GetPublicProfiles(request.UserIds, userClaims.UserId);

        GetPublicProfilesResponse response = new();
        response.Profiles.Add(profiles);

        _logger.LogTrace("Responding with {PublicProfileCount} public profiles requested by user {UserId}", response.Profiles.Count, userClaims.UserId);
        return response;
    }

    private UserClaims GetUserClaims(ServerCallContext context)
    {
        if (!context.GetHttpContext().TryGetUserClaims(_logger, out UserClaims? userClaims))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, string.Empty));
        }

        return userClaims;
    }
}
