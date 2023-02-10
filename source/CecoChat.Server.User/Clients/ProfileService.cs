using CecoChat.Contracts.User;
using CecoChat.Data.User.Repos;
using CecoChat.Server.Grpc;
using CecoChat.Server.Identity;
using Grpc.Core;

namespace CecoChat.Server.User.Clients;

public class ProfileService : Profile.ProfileBase
{
    private readonly ILogger _logger;
    private readonly IProfileRepo _repo;

    public ProfileService(ILogger<ProfileService> logger, IProfileRepo repo)
    {
        _logger = logger;
        _repo = repo;
    }

    public override async Task<GetFullProfileResponse> GetFullProfile(GetFullProfileRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaims(_logger);
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
        UserClaims userClaims = context.GetUserClaims(_logger);
        ProfilePublic? profile = await _repo.GetPublicProfile(request.UserId, userClaims.UserId);
        if (profile == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"There is no profile for user ID {request.UserId}."));
        }

        _logger.LogTrace("Responding with public profile {RequestedUserId} requested by user {UserId}", profile.UserId, userClaims.UserId);
        return new GetPublicProfileResponse { Profile = profile };
    }

    public override Task<GetPublicProfilesResponse> GetPublicProfiles(GetPublicProfilesRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaims(_logger);
        IEnumerable<ProfilePublic> profiles = _repo.GetPublicProfiles(request.UserIds, userClaims.UserId);

        GetPublicProfilesResponse response = new();
        response.Profiles.Add(profiles);

        _logger.LogTrace("Responding with {PublicProfileCount} public profiles requested by user {UserId}", response.Profiles.Count, userClaims.UserId);
        return Task.FromResult(response);
    }
}