using CecoChat.Contracts.User;

namespace CecoChat.Client.User;

public interface IUserClient : IDisposable
{
    Task<RegisterResult> Register(Registration registration, CancellationToken ct);

    Task<AuthenticateResult> Authenticate(string userName, string password, CancellationToken ct);

    Task<ChangePasswordResult> ChangePassword(string newPassword, Guid version, long userId, string accessToken, CancellationToken ct);

    Task<UpdateProfileResult> UpdateProfile(ProfileUpdate profile, long userId, string accessToken, CancellationToken ct);

    Task<ProfilePublic> GetPublicProfile(long userId, long requestedUserId, string accessToken, CancellationToken ct);

    Task<IEnumerable<ProfilePublic>> GetPublicProfiles(long userId, IEnumerable<long> requestedUserIds, string accessToken, CancellationToken ct);

    Task<IEnumerable<Connection>> GetConnections(long userId, string accessToken, CancellationToken ct);

    Task<InviteResult> Invite(long connectionId, long userId, string accessToken, CancellationToken ct);

    Task<ApproveResult> Approve(long connectionId, Guid version, long userId, string accessToken, CancellationToken ct);

    Task<CancelResult> Cancel(long connectionId, Guid version, long userId, string accessToken, CancellationToken ct);

    Task<RemoveResult> Remove(long connectionId, Guid version, long userId, string accessToken, CancellationToken ct);
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

public readonly struct ChangePasswordResult
{
    public bool Success { get; init; }
    public Guid NewVersion { get; init; }
    public bool ConcurrentlyUpdated { get; init; }
}

public readonly struct UpdateProfileResult
{
    public bool Success { get; init; }
    public Guid NewVersion { get; init; }
    public bool ConcurrentlyUpdated { get; init; }
}

public readonly struct InviteResult
{
    public bool Success { get; init; }
    public Guid Version { get; init; }
    public bool AlreadyExists { get; init; }
}

public readonly struct ApproveResult
{
    public bool Success { get; init; }
    public Guid NewVersion { get; init; }
    public bool MissingConnection { get; init; }
    public bool Invalid { get; init; }
    public bool ConcurrentlyUpdated { get; init; }
}

public readonly struct CancelResult
{
    public bool Success { get; init; }
    public bool MissingConnection { get; init; }
    public bool Invalid { get; init; }
    public bool ConcurrentlyUpdated { get; init; }
}

public readonly struct RemoveResult
{
    public bool Success { get; init; }
    public bool MissingConnection { get; init; }
    public bool Invalid { get; init; }
    public bool ConcurrentlyUpdated { get; init; }
}
