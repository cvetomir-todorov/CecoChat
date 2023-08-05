using CecoChat.Contracts.User;
using CecoChat.Data.User.Repos;
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
}
