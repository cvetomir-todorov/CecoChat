using System.Collections.Generic;

namespace CecoChat.Redis
{
    public interface IRedisOptions
    {
        List<string> Endpoints { get; }

        int ConnectRetry { get; }

        int ConnectTimeout { get; }

        int KeepAlive { get; }
    }

    public sealed class RedisOptions : IRedisOptions
    {
        public List<string> Endpoints { get; set; }

        public int ConnectRetry { get; set; }

        public int ConnectTimeout { get; set; }

        public int KeepAlive { get; set; }
    }
}
