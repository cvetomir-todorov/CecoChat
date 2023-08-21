using CecoChat.Contracts.User;

namespace CecoChat.Data.User.Profiles;

public interface IProfileCommandRepo
{
    Task<CreateProfileResult> CreateProfile(ProfileFull profile, string password);

    Task<ChangePasswordResult> ChangePassword(string newPassword, Guid version, long userId);

    Task<UpdateProfileResult> UpdateProfile(ProfileUpdate profile, long userId);
}

public readonly struct CreateProfileResult
{
    public bool Success { get; init; }
    public bool DuplicateUserName { get; init; }
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
