using CecoChat.Contracts.User;

namespace CecoChat.Client.User;

public interface IUserClient : IDisposable
{
    Task<CreateProfileResult> CreateProfile(ProfileCreate profile, CancellationToken ct);

    Task<AuthenticateResult> Authenticate(string userName, string password, CancellationToken ct);

    Task<ProfilePublic> GetPublicProfile(long userId, long requestedUserId, string accessToken, CancellationToken ct);

    Task<IEnumerable<ProfilePublic>> GetPublicProfiles(long userId, IEnumerable<long> requestedUserIds, string accessToken, CancellationToken ct);
}

public readonly struct CreateProfileResult
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
