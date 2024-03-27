using CecoChat.User.Contracts;
using Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.User.Client;

internal sealed class AuthClient : IAuthClient
{
    private readonly ILogger _logger;
    private readonly UserClientOptions _options;
    private readonly IClock _clock;
    private readonly Auth.AuthClient _authClient;

    public AuthClient(
        ILogger<AuthClient> logger,
        IOptions<UserClientOptions> options,
        IClock clock,
        Auth.AuthClient authClient)
    {
        _logger = logger;
        _options = options.Value;
        _clock = clock;
        _authClient = authClient;

        _logger.LogInformation("Auth client address set to {Address}", _options.Address);
    }

    public async Task<RegisterResult> Register(Registration registration, CancellationToken ct)
    {
        RegisterRequest request = new();
        request.Registration = registration;

        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        RegisterResponse response = await _authClient.RegisterAsync(request, deadline: deadline, cancellationToken: ct);

        if (response.Success)
        {
            _logger.LogTrace("Received successful registration for user {UserName}", registration.UserName);
            return new RegisterResult
            {
                Success = true
            };
        }
        if (response.DuplicateUserName)
        {
            _logger.LogTrace("Received failed registration for user {UserName} because of a duplicate user name", registration.UserName);
            return new RegisterResult
            {
                DuplicateUserName = true
            };
        }

        throw new ProcessingFailureException(typeof(RegisterResponse));
    }

    public async Task<AuthenticateResult> Authenticate(string userName, string password, CancellationToken ct)
    {
        AuthenticateRequest request = new();
        request.UserName = userName;
        request.Password = password;

        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        AuthenticateResponse response = await _authClient.AuthenticateAsync(request, deadline: deadline, cancellationToken: ct);

        if (response.Missing)
        {
            _logger.LogTrace("Received failed authentication for user {UserName} because of missing profile", userName);
            return new AuthenticateResult
            {
                Missing = true
            };
        }
        if (response.InvalidPassword)
        {
            _logger.LogTrace("Received failed authentication for user {UserName} because of invalid password", userName);
            return new AuthenticateResult
            {
                InvalidPassword = true
            };
        }
        if (response.Profile != null)
        {
            _logger.LogTrace("Received successful authentication and a full profile for user {UserId} named {UserName}", response.Profile.UserId, response.Profile.UserName);
            return new AuthenticateResult
            {
                Profile = response.Profile
            };
        }

        throw new ProcessingFailureException(typeof(AuthenticateResponse));
    }
}
