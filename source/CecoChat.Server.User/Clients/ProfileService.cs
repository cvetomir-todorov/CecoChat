using System.Diagnostics;
using CecoChat.Contracts.User;
using CecoChat.Data.User.Repos;
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
        long userId = GetUserId(context);
        ProfileFull? profile = await _repo.GetFullProfile(userId);
        if (profile == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"There is no profile for user ID {userId}."));
        }

        _logger.LogTrace("Responding with full profile {ProfileUserName} for user {UserId}", profile.UserName, userId);
        return new GetFullProfileResponse { Profile = profile };
    }

    public override async Task<GetPublicProfileResponse> GetPublicProfile(GetPublicProfileRequest request, ServerCallContext context)
    {
        long userId = GetUserId(context);
        ProfilePublic? profile = await _repo.GetPublicProfile(request.UserId, userId);
        if (profile == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"There is no profile for user ID {request.UserId}."));
        }

        _logger.LogTrace("Responding with public profile {RequestedUserId} requested by user {UserId}", profile.UserId, userId);
        return new GetPublicProfileResponse { Profile = profile };
    }

    public override Task<GetPublicProfilesResponse> GetPublicProfiles(GetPublicProfilesRequest request, ServerCallContext context)
    {
        long userId = GetUserId(context);
        IEnumerable<ProfilePublic> profiles = _repo.GetPublicProfiles(request.UserIds, userId);

        GetPublicProfilesResponse response = new();
        response.Profiles.Add(profiles);

        _logger.LogTrace("Responding with {PublicProfileCount} public profiles requested by user {UserId}", response.Profiles.Count, userId);
        return Task.FromResult(response);
    }

    // TODO: reuse this across all grpc services
    private static long GetUserId(ServerCallContext context)
    {
        if (!context.GetHttpContext().User.TryGetUserId(out long userId))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Client has no parseable access token."));
        }
        Activity.Current?.SetTag("user.id", userId);
        return userId;
    }
}