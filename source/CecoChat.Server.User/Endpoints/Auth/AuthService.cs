using CecoChat.Data.User.Entities.Profiles;
using CecoChat.Server.User.Security;
using CecoChat.User.Contracts;
using Common;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace CecoChat.Server.User.Endpoints.Auth;

public class AuthService : CecoChat.User.Contracts.Auth.AuthBase
{
    private readonly ILogger _logger;
    private readonly IProfileQueryRepo _queryRepo;
    private readonly IProfileCommandRepo _commandRepo;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
        ILogger<AuthService> logger,
        IProfileQueryRepo queryRepo,
        IProfileCommandRepo commandRepo,
        IPasswordHasher passwordHasher)
    {
        _logger = logger;
        _queryRepo = queryRepo;
        _commandRepo = commandRepo;
        _passwordHasher = passwordHasher;
    }

    [AllowAnonymous]
    public override async Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
    {
        string hashedPassword = _passwordHasher.Hash(request.Registration.Password);

        ProfileFull profile = new()
        {
            UserName = request.Registration.UserName,
            DisplayName = request.Registration.DisplayName,
            AvatarUrl = request.Registration.AvatarUrl,
            Phone = request.Registration.Phone,
            Email = request.Registration.Email
        };
        CreateProfileResult result = await _commandRepo.CreateProfile(profile, hashedPassword);

        if (result.Success)
        {
            _logger.LogTrace("Responding with successful registration for user {UserName}", request.Registration.UserName);
            return new RegisterResponse
            {
                Success = true
            };
        }
        if (result.DuplicateUserName)
        {
            _logger.LogTrace("Responding with failed registration for user {UserName} because of duplicate user name", request.Registration.UserName);
            return new RegisterResponse
            {
                DuplicateUserName = true
            };
        }

        throw new ProcessingFailureException(typeof(CreateProfileResult));
    }

    [AllowAnonymous]
    public override async Task<AuthenticateResponse> Authenticate(AuthenticateRequest request, ServerCallContext context)
    {
        FullProfileResult result = await _queryRepo.GetFullProfile(request.UserName, includePassword: true);
        if (!result.Success)
        {
            _logger.LogTrace("Responding with missing profile for user {UserName}", request.UserName);
            return new AuthenticateResponse
            {
                Missing = true
            };
        }
        if (result.Profile == null || string.IsNullOrWhiteSpace(result.Password))
        {
            throw new InvalidOperationException("Profile should not be null and password should not be null or whitespace.");
        }

        bool isValid = _passwordHasher.Verify(request.Password, result.Password);
        if (!isValid)
        {
            _logger.LogTrace("Responding with invalid password for user {UserName}", request.UserName);
            return new AuthenticateResponse
            {
                InvalidPassword = true
            };
        }

        _logger.LogTrace("Responding with successful authentication for user {UserId} named {UserName}", result.Profile.UserId, result.Profile.UserName);
        return new AuthenticateResponse
        {
            Profile = result.Profile
        };
    }
}
