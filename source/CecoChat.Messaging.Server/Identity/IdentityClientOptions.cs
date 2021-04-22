using System;

namespace CecoChat.Messaging.Server.Identity
{
    public interface IIdentityClientOptions
    {
        int RefreshIDsCount { get; }

        TimeSpan GetIDWaitInterval { get; }

        TimeSpan InvalidateIDsInterval {get;}

        Uri Address { get; }

        TimeSpan KeepAlivePingDelay { get; }

        TimeSpan KeepAlivePingTimeout { get; }

        TimeSpan CallTimeout { get; }

        int RetryCount { get; }

        TimeSpan InitialBackOff { get; }

        double BackOffMultiplier {get;}

        TimeSpan MaxBackOff { get; }

        int MaxJitterMs { get; }
    }

    public sealed class IdentityClientOptions : IIdentityClientOptions
    {
        public int RefreshIDsCount { get; set; }

        public TimeSpan GetIDWaitInterval { get; set; }

        public TimeSpan InvalidateIDsInterval { get; set; }

        public Uri Address { get; set; }

        public TimeSpan KeepAlivePingDelay { get; set; }

        public TimeSpan KeepAlivePingTimeout { get; set; }

        public TimeSpan CallTimeout { get; set; }

        public int RetryCount { get; set; }

        public TimeSpan InitialBackOff { get; set; }

        public double BackOffMultiplier { get; set; }

        public TimeSpan MaxBackOff { get; set; }

        public int MaxJitterMs { get; set; }
    }
}
