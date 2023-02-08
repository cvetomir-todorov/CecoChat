namespace CecoChat.Npgsql;

public class NpgsqlOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    public TimeSpan CommandTimeout { get; set; }
}
