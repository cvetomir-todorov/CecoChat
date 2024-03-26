using Common.Npgsql;

namespace CecoChat.Server.User;

public sealed class UserDbOptions
{
    public NpgsqlOptions Init { get; init; } = new();
    public bool Seed { get; init; }
    public bool SeedConsoleClientUsers { get; init; }
    public bool SeedLoadTestingUsers { get; init; }
    public int SeedLoadTestingUserCount { get; init; }
    public NpgsqlOptions Connect { get; init; } = new();
}
