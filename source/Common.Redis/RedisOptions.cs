namespace Common.Redis;

public sealed class RedisOptions
{
    public string Name { get; init; } = string.Empty;

    public string[] Endpoints { get; init; } = Array.Empty<string>();

    public int ConnectRetry { get; init; }

    public int ConnectTimeout { get; init; }

    public int KeepAlive { get; init; }

    public TimeSpan HealthTimeout { get; init; }
}
