using CecoChat.Contracts.User;

namespace CecoChat.Client.User;

public interface IProfileClient
{
    Task<ChangePasswordResult> ChangePassword(string newPassword, Guid version, long userId, string accessToken, CancellationToken ct);

    Task<UpdateProfileResult> UpdateProfile(ProfileUpdate profile, long userId, string accessToken, CancellationToken ct);

    Task<ProfilePublic> GetPublicProfile(long userId, long requestedUserId, string accessToken, CancellationToken ct);

    Task<IEnumerable<ProfilePublic>> GetPublicProfiles(long userId, IEnumerable<long> requestedUserIds, string accessToken, CancellationToken ct);
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