using CecoChat.DynamicConfig.Sections.User;
using CecoChat.Server.Identity;
using CecoChat.User.Contracts;
using CecoChat.User.Data.Entities.Profiles;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace CecoChat.User.Service.Endpoints.Profiles;

public class ProfileQueryService : ProfileQuery.ProfileQueryBase
{
    private readonly ILogger _logger;
    private readonly IProfileQueryRepo _repo;
    private readonly IUserConfig _userConfig;

    public ProfileQueryService(
        ILogger<ProfileQueryService> logger,
        IProfileQueryRepo repo,
        IUserConfig userConfig)
    {
        _logger = logger;
        _repo = repo;
        _userConfig = userConfig;
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

        IEnumerable<ProfilePublic> profiles = await _repo.GetPublicProfiles(request.SearchPattern, _userConfig.ProfileCount, userClaims.UserId);

        GetPublicProfilesByPatternResponse response = new();
        response.Profiles.Add(profiles);

        _logger.LogTrace(
            "Responding with {PublicProfileCount} public profiles matching the search pattern {ProfileSearchPattern} as requested by user {UserId}",
            response.Profiles.Count, request.SearchPattern, userClaims.UserId);
        return response;
    }
}
