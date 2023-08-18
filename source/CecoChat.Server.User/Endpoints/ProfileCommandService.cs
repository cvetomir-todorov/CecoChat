using CecoChat.Contracts;
using CecoChat.Contracts.User;
using CecoChat.Data.User.Profiles;
using CecoChat.Server.Identity;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace CecoChat.Server.User.Endpoints;

public class ProfileCommandService : ProfileCommand.ProfileCommandBase
{
    private readonly ILogger _logger;
    private readonly IProfileCommandRepo _profileRepo;

    public ProfileCommandService(ILogger<ProfileCommandService> logger, IProfileCommandRepo profileRepo)
    {
        _logger = logger;
        _profileRepo = profileRepo;
    }

    [AllowAnonymous]
    public override async Task<CreateProfileResponse> CreateProfile(CreateProfileRequest request, ServerCallContext context)
    {
        CreateProfileResult result = await _profileRepo.CreateProfile(request.Profile);

        if (result.Success)
        {
            _logger.LogTrace("Responding with successful profile creation for user {UserName}", request.Profile.UserName);
            return new CreateProfileResponse { Success = true };
        }
        if (result.DuplicateUserName)
        {
            _logger.LogTrace("Responding with unsuccessful profile creation for user {UserName} because of duplicate user name", request.Profile.UserName);
            return new CreateProfileResponse { DuplicateUserName = true };
        }

        throw new InvalidOperationException($"Failed to process {nameof(CreateProfileResult)}.");
    }

    [Authorize(Policy = "user")]
    public override async Task<ChangePasswordResponse> ChangePassword(ChangePasswordRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        ChangePasswordResult result = await _profileRepo.ChangePassword(request.Profile, userClaims.UserId);

        if (result.Success)
        {
            _logger.LogTrace("Responding with successful password change for user {UserId}", userClaims.UserId);
            return new ChangePasswordResponse
            {
                Success = true,
                NewVersion = result.NewVersion.ToUuid()
            };
        }
        if (result.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Responding with failed password change for user {UserId} since the profile has been concurrently updated", userClaims.UserId);
            return new ChangePasswordResponse { ConcurrentlyUpdated = true };
        }

        throw new InvalidOperationException($"Failed to process {nameof(ChangePasswordResult)}.");
    }

    [Authorize(Policy = "user")]
    public override async Task<UpdateProfileResponse> UpdateProfile(UpdateProfileRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        UpdateProfileResult result = await _profileRepo.UpdateProfile(request.Profile, userClaims.UserId);

        if (result.Success)
        {
            _logger.LogTrace("Responding with successful profile update for user {UserId}", userClaims.UserId);
            return new UpdateProfileResponse
            {
                Success = true,
                NewVersion = result.NewVersion.ToUuid()
            };
        }
        if (result.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Responding with failed profile update for user {UserId} since it has been concurrently updated", userClaims.UserId);
            return new UpdateProfileResponse { ConcurrentlyUpdated = true };
        }

        throw new InvalidOperationException($"Failed to process {nameof(UpdateProfileResult)}.");
    }
}
