using CecoChat.Npgsql;

namespace CecoChat.Server.User.HostedServices;

public sealed class UserDbOptions
{
    public NpgsqlOptions Init { get; set; } = new();
    public bool Seed { get; set; }
    public bool SeedConsoleClientUsers { get; set; }
    public bool SeedLoadTestingUsers { get; set; }
    public int SeedLoadTestingUserCount { get; set; }
    public NpgsqlOptions Connect { get; set; } = new();
}
