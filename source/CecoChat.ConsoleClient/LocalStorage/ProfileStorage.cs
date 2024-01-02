using System.Collections.Concurrent;

namespace CecoChat.ConsoleClient.LocalStorage;

public class ProfileStorage
{
    private readonly ConcurrentDictionary<long, ProfilePublic> _profilesMap;

    public ProfileStorage()
    {
        _profilesMap = new();
    }

    public IEnumerable<ProfilePublic> EnumerateProfiles()
    {
        foreach (KeyValuePair<long, ProfilePublic> pair in _profilesMap)
        {
            yield return pair.Value;
        }
    }

    public void AddOrUpdateProfiles(IEnumerable<ProfilePublic> profiles)
    {
        foreach (ProfilePublic profile in profiles)
        {
            AddOrUpdateProfile(profile);
        }
    }

    public void AddOrUpdateProfile(ProfilePublic profile)
    {
        if (!_profilesMap.TryAdd(profile.UserId, profile))
        {
            _profilesMap[profile.UserId] = profile;
        }
    }

    public ProfilePublic GetProfile(long userId)
    {
        if (!_profilesMap.TryGetValue(userId, out ProfilePublic? profile))
        {
            throw new InvalidOperationException($"Could not find profile with ID {userId}.");
        }

        return profile;
    }
}
