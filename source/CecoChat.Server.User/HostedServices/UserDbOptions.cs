using CecoChat.Npgsql;

namespace CecoChat.Server.User.HostedServices;

public sealed class UserDbOptions
{
    public NpgsqlOptions Init { get; set; } = new();
    public bool Seed { get; set; }
    public NpgsqlOptions Connect { get; set; } = new();
}
