using CecoChat.Contracts.User;

namespace CecoChat.Data.User.Repos;

public interface IProfileRepo
{
    Task<ProfileFull?> GetFullProfile(long requestedUserId);

    Task<ProfilePublic?> GetPublicProfile(long requestedUserId, long userId);

    Task<IEnumerable<ProfilePublic>> GetPublicProfiles(IList<long> requestedUserIds, long userId);
}
