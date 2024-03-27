using CecoChat.User.Contracts;

namespace CecoChat.Client.User;

public interface IProfileClient
{
    Task<ChangePasswordResult> ChangePassword(string newPassword, DateTime version, long userId, string accessToken, CancellationToken ct);

    Task<UpdateProfileResult> UpdateProfile(ProfileUpdate profile, long userId, string accessToken, CancellationToken ct);

    Task<ProfilePublic?> GetPublicProfile(long userId, long requestedUserId, string accessToken, CancellationToken ct);

    Task<IReadOnlyCollection<ProfilePublic>> GetPublicProfiles(long userId, IEnumerable<long> requestedUserIds, string accessToken, CancellationToken ct);

    Task<IReadOnlyCollection<ProfilePublic>> GetPublicProfiles(long userId, string searchPattern, string accessToken, CancellationToken ct);
}

public readonly struct ChangePasswordResult
{
    public bool Success { get; init; }
    public DateTime NewVersion { get; init; }
    public bool ConcurrentlyUpdated { get; init; }
}

public readonly struct UpdateProfileResult
{
    public bool Success { get; init; }
    public DateTime NewVersion { get; init; }
    public bool ConcurrentlyUpdated { get; init; }
}
