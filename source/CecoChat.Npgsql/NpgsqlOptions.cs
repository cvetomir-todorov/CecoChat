namespace CecoChat.Npgsql;

public class NpgsqlOptions
{
    public string ConnectionString { get; init; } = string.Empty;

    public TimeSpan HealthTimeout { get; init; }
}
