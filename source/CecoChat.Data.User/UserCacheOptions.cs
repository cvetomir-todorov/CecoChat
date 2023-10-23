using CecoChat.Redis;

namespace CecoChat.Data.User;

public sealed class UserCacheOptions
{
    public bool Enabled { get; init; }

    public int ProfilesDatabase { get; init; }

    public int AsyncProfileProcessors { get; init; }

    public TimeSpan ProfileEntryDuration { get; init; }

    public int ConnectionsDatabase { get; init; }

    public TimeSpan ConnectionEntriesDuration { get; init; }

    public RedisOptions Store { get; init; } = new();
}
