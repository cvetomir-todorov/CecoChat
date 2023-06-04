using CecoChat.Npgsql;

namespace CecoChat.Server.User.HostedServices;

public sealed class UserDbOptions
{
    public NpgsqlOptions Init { get; init; } = new();
    public bool Seed { get; init; }
    public bool SeedConsoleClientUsers { get; init; }
    public bool SeedLoadTestingUsers { get; init; }
    public int SeedLoadTestingUserCount { get; init; }
    public NpgsqlOptions Connect { get; init; } = new();
}
