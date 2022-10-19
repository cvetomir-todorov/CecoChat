namespace CecoChat.Cassandra;

public sealed class CassandraOptions
{
    public string[] ContactPoints { get; set; } = Array.Empty<string>();

    public string LocalDC { get; set; } = string.Empty;

    public TimeSpan SocketConnectTimeout { get; set; }

    public bool ExponentialReconnectPolicy { get; set; }

    public TimeSpan ExponentialReconnectPolicyBaseDelay { get; set; }

    public TimeSpan ExponentialReconnectPolicyMaxDelay { get; set; }
}