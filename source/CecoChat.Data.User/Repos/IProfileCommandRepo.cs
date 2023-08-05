using CecoChat.Contracts.User;

namespace CecoChat.Data.User.Repos;

public readonly struct CreateProfileResult
{
    public bool Success { get; init; }

    public bool DuplicateUserName { get; init; }
}

public interface IProfileCommandRepo
{
    Task<CreateProfileResult> CreateProfile(ProfileCreate profile);
}
