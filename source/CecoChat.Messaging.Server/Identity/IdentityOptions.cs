using System;

namespace CecoChat.Messaging.Server.Identity
{
    public interface IIdentityOptions
    {
        Uri Address { get; }

        TimeSpan CallTimeout { get; }

        int RetryTimes { get; }

        TimeSpan InitialBackOff { get; }

        int BackOffMultiplier {get;}

        TimeSpan MaxBackOff { get; }
    }

    public sealed class IdentityOptions : IIdentityOptions
    {
        public Uri Address { get; set; }

        public TimeSpan CallTimeout { get; set; }

        public int RetryTimes { get; set; }

        public TimeSpan InitialBackOff { get; set; }

        public int BackOffMultiplier { get; set; }

        public TimeSpan MaxBackOff { get; set; }
    }
}
