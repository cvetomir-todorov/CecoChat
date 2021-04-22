using System;

namespace CecoChat.Messaging.Server.Identity
{
    public interface IIdentityOptions
    {
        IIdentityGenerationOptions Generation { get; }
        IIdentityCommunicationOptions Communication { get; }
        IIdentityRetryOptions Retry { get; }
    }

    public sealed class IdentityOptions : IIdentityOptions
    {
        IIdentityGenerationOptions IIdentityOptions.Generation => Generation;
        IIdentityCommunicationOptions IIdentityOptions.Communication => Communication;
        IIdentityRetryOptions IIdentityOptions.Retry => Retry;

        public IdentityGenerationOptions Generation { get; set; }
        public IdentityCommunicationOptions Communication { get; set; }
        public IdentityRetryOptions Retry { get; set; }
    }

    public interface IIdentityGenerationOptions
    {
        int RefreshIDsCount { get; }
        TimeSpan GetIDWaitInterval { get; }
        TimeSpan InvalidateIDsInterval {get;}
    }

    public sealed class IdentityGenerationOptions : IIdentityGenerationOptions
    {
        public int RefreshIDsCount { get; set; }
        public TimeSpan GetIDWaitInterval { get; set; }
        public TimeSpan InvalidateIDsInterval { get; set; }
    }

    public interface IIdentityCommunicationOptions
    {
        Uri Address { get; }
        TimeSpan KeepAlivePingDelay { get; }
        TimeSpan KeepAlivePingTimeout { get; }
        TimeSpan CallTimeout { get; }
    }

    public sealed class IdentityCommunicationOptions : IIdentityCommunicationOptions
    {
        public Uri Address { get; set; }
        public TimeSpan KeepAlivePingDelay { get; set; }
        public TimeSpan KeepAlivePingTimeout { get; set; }
        public TimeSpan CallTimeout { get; set; }
    }

    public interface IIdentityRetryOptions
    {
        int RetryCount { get; }
        TimeSpan InitialBackOff { get; }
        double BackOffMultiplier {get;}
        TimeSpan MaxBackOff { get; }
        int MaxJitterMs { get; }
    }

    public sealed class IdentityRetryOptions : IIdentityRetryOptions
    {
        public int RetryCount { get; set; }
        public TimeSpan InitialBackOff { get; set; }
        public double BackOffMultiplier { get; set; }
        public TimeSpan MaxBackOff { get; set; }
        public int MaxJitterMs { get; set; }
    }
}
