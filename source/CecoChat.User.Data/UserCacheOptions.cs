using Common.Redis;

namespace CecoChat.User.Data;

public sealed class UserCacheOptions
{
    public bool Enabled { get; init; }

    public int AsyncProfileProcessors { get; init; }

    public TimeSpan ProfileEntryDuration { get; init; }

    public TimeSpan ProfileSearchDuration { get; init; }

    public TimeSpan ConnectionEntriesDuration { get; init; }

    public RedisOptions Store { get; init; } = new();
}
