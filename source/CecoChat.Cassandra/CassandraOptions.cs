namespace CecoChat.Cassandra;

public sealed class CassandraOptions
{
    public string[] ContactPoints { get; init; } = Array.Empty<string>();

    public string LocalDc { get; init; } = string.Empty;

    public TimeSpan SocketConnectTimeout { get; init; }

    public bool ExponentialReconnectPolicy { get; init; }

    public TimeSpan ExponentialReconnectPolicyBaseDelay { get; init; }

    public TimeSpan ExponentialReconnectPolicyMaxDelay { get; init; }

    public TimeSpan HealthTimeout { get; init; }
}
