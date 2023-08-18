using CecoChat.Contracts.User;

namespace CecoChat.Data.User.Profiles;

public interface IProfileQueryRepo
{
    Task<AuthenticateResult> Authenticate(string userName, string password);

    Task<ProfilePublic?> GetPublicProfile(long requestedUserId, long userId);

    Task<IEnumerable<ProfilePublic>> GetPublicProfiles(IList<long> requestedUserIds, long userId);
}

public readonly struct AuthenticateResult
{
    public bool Missing { get; init; }

    public bool InvalidPassword { get; init; }

    public ProfileFull? Profile { get; init; }

    public bool Success => Profile != null;
}
