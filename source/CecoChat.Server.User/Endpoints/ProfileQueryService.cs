using CecoChat.Contracts.User;
using CecoChat.Data.User.Profiles;
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

    [Authorize(Policy = "user")]
    public override async Task<GetPublicProfileResponse> GetPublicProfile(GetPublicProfileRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        ProfilePublic? profile = await _repo.GetPublicProfile(request.UserId, userClaims.UserId);
        if (profile == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"There is no profile for user ID {request.UserId}."));
        }

        _logger.LogTrace("Responding with public profile {RequestedUserId} requested by user {UserId}", profile.UserId, userClaims.UserId);
        return new GetPublicProfileResponse { Profile = profile };
    }

    [Authorize(Policy = "user")]
    public override async Task<GetPublicProfilesByIdListResponse> GetPublicProfilesByIdList(GetPublicProfilesByIdListRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        IEnumerable<ProfilePublic> profiles = await _repo.GetPublicProfiles(request.UserIds, userClaims.UserId);

        GetPublicProfilesByIdListResponse response = new();
        response.Profiles.Add(profiles);

        _logger.LogTrace("Responding with {PublicProfileCount} public profiles in the ID list requested by user {UserId}", response.Profiles.Count, userClaims.UserId);
        return response;
    }

    [Authorize(Policy = "user")]
    public override async Task<GetPublicProfilesByPatternResponse> GetPublicProfilesByPattern(GetPublicProfilesByPatternRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        IEnumerable<ProfilePublic> profiles = await _repo.GetPublicProfiles(request.SearchPattern, userClaims.UserId);

        GetPublicProfilesByPatternResponse response = new();
        response.Profiles.Add(profiles);

        _logger.LogTrace(
            "Responding with {PublicProfileCount} public profiles matching the search pattern {ProfileSearchPattern} as requested by user {UserId}",
            response.Profiles.Count, request.SearchPattern, userClaims.UserId);
        return response;
    }
}
