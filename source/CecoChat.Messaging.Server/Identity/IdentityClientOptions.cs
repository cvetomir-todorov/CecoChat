using System;

namespace CecoChat.Messaging.Server.Identity
{
    public interface IIdentityClientOptions
    {
        Uri Address { get; }

        TimeSpan CallTimeout { get; }

        int RetryTimes { get; }

        TimeSpan InitialBackOff { get; }

        double BackOffMultiplier {get;}

        TimeSpan MaxBackOff { get; }
    }

    public sealed class IdentityClientOptions : IIdentityClientOptions
    {
        public Uri Address { get; set; }

        public TimeSpan CallTimeout { get; set; }

        public int RetryTimes { get; set; }

        public TimeSpan InitialBackOff { get; set; }

        public double BackOffMultiplier { get; set; }

        public TimeSpan MaxBackOff { get; set; }
    }
}
