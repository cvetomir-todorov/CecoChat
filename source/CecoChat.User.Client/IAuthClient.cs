using CecoChat.User.Contracts;

namespace CecoChat.User.Client;

public interface IAuthClient
{
    Task<RegisterResult> Register(Registration registration, CancellationToken ct);

    Task<AuthenticateResult> Authenticate(string userName, string password, CancellationToken ct);
}

public readonly struct RegisterResult
{
    public bool Success { get; init; }
    public bool DuplicateUserName { get; init; }
}

public readonly struct AuthenticateResult
{
    public bool Missing { get; init; }
    public bool InvalidPassword { get; init; }
    public ProfileFull? Profile { get; init; }
}
