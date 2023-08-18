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

    [AllowAnonymous]
    public override async Task<AuthenticateResponse> Authenticate(AuthenticateRequest request, ServerCallContext context)
    {
        AuthenticateResult authenticateResult = await _repo.Authenticate(request.UserName, request.Password);
        if (authenticateResult.Missing)
        {
            _logger.LogTrace("Responding with missing profile for user {UserName}", request.UserName);
            return new AuthenticateResponse { Missing = true };
        }
        if (authenticateResult.InvalidPassword)
        {
            _logger.LogTrace("Responding with invalid password for user {UserName}", request.UserName);
            return new AuthenticateResponse { InvalidPassword = true };
        }
        if (authenticateResult.Profile != null)
        {
            _logger.LogTrace("Responding after successful authentication with full profile for user {UserId} named {UserName}",
                authenticateResult.Profile.UserId, authenticateResult.Profile.UserName);
            return new AuthenticateResponse { Profile = authenticateResult.Profile };
        }

        throw new ProcessingFailureException(typeof(AuthenticateResult));
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
    public override async Task<GetPublicProfilesResponse> GetPublicProfiles(GetPublicProfilesRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        IEnumerable<ProfilePublic> profiles = await _repo.GetPublicProfiles(request.UserIds, userClaims.UserId);

        GetPublicProfilesResponse response = new();
        response.Profiles.Add(profiles);

        _logger.LogTrace("Responding with {PublicProfileCount} public profiles requested by user {UserId}", response.Profiles.Count, userClaims.UserId);
        return response;
    }
}
