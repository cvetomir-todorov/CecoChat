namespace CecoChat.Redis;

public sealed class RedisOptions
{
    public string[] Endpoints { get; set; } = Array.Empty<string>();

    public int ConnectRetry { get; set; }

    public int ConnectTimeout { get; set; }

    public int KeepAlive { get; set; }

    public TimeSpan HealthTimeout { get; set; }
}
