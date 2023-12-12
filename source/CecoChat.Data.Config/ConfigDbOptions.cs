using CecoChat.Npgsql;

namespace CecoChat.Data.Config;

public sealed class ConfigDbOptions
{
    public NpgsqlOptions Init { get; init; } = new();
    public NpgsqlOptions Connect { get; init; } = new();
    public string DeploymentEnvironment { get; set; } = string.Empty;
}
