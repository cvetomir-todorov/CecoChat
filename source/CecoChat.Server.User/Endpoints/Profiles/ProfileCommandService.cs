using CecoChat.Server.Identity;
using CecoChat.Server.User.Security;
using CecoChat.User.Contracts;
using CecoChat.User.Data.Entities.Profiles;
using Common;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace CecoChat.Server.User.Endpoints.Profiles;

public class ProfileCommandService : ProfileCommand.ProfileCommandBase
{
    private readonly ILogger _logger;
    private readonly IProfileCommandRepo _commandRepo;
    private readonly IPasswordHasher _passwordHasher;

    public ProfileCommandService(
        ILogger<ProfileCommandService> logger,
        IProfileCommandRepo commandRepo,
        IPasswordHasher passwordHasher)
    {
        _logger = logger;
        _commandRepo = commandRepo;
        _passwordHasher = passwordHasher;
    }

    [Authorize(Policy = "user")]
    public override async Task<ChangePasswordResponse> ChangePassword(ChangePasswordRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        string passwordHashAndSalt = _passwordHasher.Hash(request.NewPassword);
        ChangePasswordResult result = await _commandRepo.ChangePassword(passwordHashAndSalt, request.Version.ToDateTime(), userClaims.UserId);

        if (result.Success)
        {
            _logger.LogTrace("Responding with successful password change for user {UserId}", userClaims.UserId);
            return new ChangePasswordResponse
            {
                Success = true,
                NewVersion = result.NewVersion.ToTimestamp()
            };
        }
        if (result.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Responding with failed password change for user {UserId} because the profile has been concurrently updated", userClaims.UserId);
            return new ChangePasswordResponse { ConcurrentlyUpdated = true };
        }

        throw new ProcessingFailureException(typeof(ChangePasswordResult));
    }

    [Authorize(Policy = "user")]
    public override async Task<UpdateProfileResponse> UpdateProfile(UpdateProfileRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        UpdateProfileResult result = await _commandRepo.UpdateProfile(request.Profile, userClaims.UserId);

        if (result.Success)
        {
            _logger.LogTrace("Responding with successful profile update for user {UserId}", userClaims.UserId);
            return new UpdateProfileResponse
            {
                Success = true,
                NewVersion = result.NewVersion.ToTimestamp()
            };
        }
        if (result.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Responding with failed profile update for user {UserId} since it has been concurrently updated", userClaims.UserId);
            return new UpdateProfileResponse { ConcurrentlyUpdated = true };
        }

        throw new ProcessingFailureException(typeof(UpdateProfileResult));
    }
}
