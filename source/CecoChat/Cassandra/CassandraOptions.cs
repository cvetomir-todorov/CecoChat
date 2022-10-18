using System;
using System.Collections.Generic;

namespace CecoChat.Cassandra;

public sealed class CassandraOptions
{
    public List<string> ContactPoints { get; set; }

    public string LocalDC { get; set; }

    public TimeSpan SocketConnectTimeout { get; set; }

    public bool ExponentialReconnectPolicy { get; set; }

    public TimeSpan ExponentialReconnectPolicyBaseDelay { get; set; }

    public TimeSpan ExponentialReconnectPolicyMaxDelay { get; set; }
}