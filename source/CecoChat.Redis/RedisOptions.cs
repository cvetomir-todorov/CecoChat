namespace CecoChat.Redis;

public sealed class RedisOptions
{
    public string[] Endpoints { get; init; } = Array.Empty<string>();

    public int ConnectRetry { get; init; }

    public int ConnectTimeout { get; init; }

    public int KeepAlive { get; init; }

    public int DefaultDatabase { get; init; }

    public TimeSpan HealthTimeout { get; init; }
}
