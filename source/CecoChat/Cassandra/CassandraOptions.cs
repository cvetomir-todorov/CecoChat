using System;
using System.Collections.Generic;

namespace CecoChat.Cassandra
{
    public interface ICassandraOptions
    {
        List<string> ContactPoints { get; }

        string LocalDC { get; }

        TimeSpan SocketConnectTimeout { get; }

        bool ExponentialReconnectPolicy { get; }

        TimeSpan ExponentialReconnectPolicyBaseDelay { get; }

        TimeSpan ExponentialReconnectPolicyMaxDelay { get; }
    }

    public sealed class CassandraOptions : ICassandraOptions
    {
        public List<string> ContactPoints { get; set; }

        public string LocalDC { get; set; }

        public TimeSpan SocketConnectTimeout { get; set; }

        public bool ExponentialReconnectPolicy { get; set; }

        public TimeSpan ExponentialReconnectPolicyBaseDelay { get; set; }

        public TimeSpan ExponentialReconnectPolicyMaxDelay { get; set; }
    }
}
