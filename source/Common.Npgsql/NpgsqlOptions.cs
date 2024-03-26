namespace Common.Npgsql;

public class NpgsqlOptions
{
    public string ConnectionString { get; init; } = string.Empty;

    public bool EnableRetryOnFailure { get; init; }

    public int MaxRetryCount { get; init; }

    public TimeSpan MaxRetryDelay { get; init; }

    public int DbContextPoolSize { get; init; }

    public TimeSpan HealthTimeout { get; init; }
}
