using CecoChat.Contracts.User;

namespace CecoChat.Client.User;

public interface IUserClient : IDisposable
{
    Task<CreateProfileResult> CreateProfile(ProfileCreate profile, CancellationToken ct);

    Task<AuthenticateResult> Authenticate(string userName, string password, CancellationToken ct);

    Task<ChangePasswordResult> ChangePassword(ProfileChangePassword profile, long userId, string accessToken, CancellationToken ct);

    Task<UpdateProfileResult> UpdateProfile(ProfileUpdate profile, long userId, string accessToken, CancellationToken ct);

    Task<ProfilePublic> GetPublicProfile(long userId, long requestedUserId, string accessToken, CancellationToken ct);

    Task<IEnumerable<ProfilePublic>> GetPublicProfiles(long userId, IEnumerable<long> requestedUserIds, string accessToken, CancellationToken ct);

    Task<IEnumerable<Contact>> GetContacts(long userId, string accessToken, CancellationToken ct);

    Task<InviteContactResult> InviteContact(long contactUserId, long userId, string accessToken, CancellationToken ct);

    Task<ApproveContactResult> ApproveContact(long contactUserId, Guid version, long userId, string accessToken, CancellationToken ct);

    Task<CancelContactResult> CancelContact(long contactUserId, Guid version, long userId, string accessToken, CancellationToken ct);

    Task<RemoveContactResult> RemoveContact(long contactUserId, Guid version, long userId, string accessToken, CancellationToken ct);
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

public readonly struct InviteContactResult
{
    public bool Success { get; init; }
    public Guid Version { get; init; }
    public bool AlreadyExists { get; init; }
}

public readonly struct ApproveContactResult
{
    public bool Success { get; init; }
    public Guid NewVersion { get; init; }
    public bool MissingContact { get; init; }
    public bool Invalid { get; init; }
    public bool ConcurrentlyUpdated { get; init; }
}

public readonly struct CancelContactResult
{
    public bool Success { get; init; }
    public bool MissingContact { get; init; }
    public bool Invalid { get; init; }
    public bool ConcurrentlyUpdated { get; init; }
}

public readonly struct RemoveContactResult
{
    public bool Success { get; init; }
    public bool MissingContact { get; init; }
    public bool Invalid { get; init; }
    public bool ConcurrentlyUpdated { get; init; }
}
