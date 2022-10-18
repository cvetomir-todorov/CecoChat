namespace CecoChat.Redis;

public sealed class RedisOptions
{
    public List<string> Endpoints { get; set; }

    public int ConnectRetry { get; set; }

    public int ConnectTimeout { get; set; }

    public int KeepAlive { get; set; }
}