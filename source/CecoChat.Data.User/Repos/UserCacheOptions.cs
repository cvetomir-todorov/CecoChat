using CecoChat.Redis;

namespace CecoChat.Data.User.Repos;

public sealed class UserCacheOptions
{
    public bool Enabled { get; init; }

    public int AsyncProfileProcessors { get; init; }

    public TimeSpan ProfileEntryDuration { get; init; }

    public RedisOptions Store { get; init; } = new();
}
