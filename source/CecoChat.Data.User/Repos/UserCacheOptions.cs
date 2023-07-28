using CecoChat.Redis;

namespace CecoChat.Data.User.Repos;

public sealed class UserCacheOptions
{
    public bool Enabled { get; init; }

    public RedisOptions Db { get; init; } = new();
}
