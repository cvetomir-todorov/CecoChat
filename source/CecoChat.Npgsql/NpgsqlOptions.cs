namespace CecoChat.Npgsql;

public class NpgsqlOptions
{
    public string ConnectionString { get; init; } = string.Empty;

    public int DbContextPoolSize { get; init; }

    public TimeSpan HealthTimeout { get; init; }
}
