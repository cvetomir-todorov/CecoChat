using System;

namespace CecoChat.Client.IDGen
{
    public sealed class IDGenOptions
    {
        public IDGenGenerationOptions Generation { get; set; }
        public IDGenCommunicationOptions Communication { get; set; }
        public IDGenRetryOptions Retry { get; set; }
    }

    public sealed class IDGenGenerationOptions
    {
        public int RefreshIDsCount { get; set; }
        public int MaxConcurrentRequests { get; set; }
        public TimeSpan GetIDWaitInterval { get; set; }
        public TimeSpan InvalidateIDsInterval { get; set; }
    }

    public sealed class IDGenCommunicationOptions
    {
        public Uri Address { get; set; }
        public TimeSpan KeepAlivePingDelay { get; set; }
        public TimeSpan KeepAlivePingTimeout { get; set; }
        public TimeSpan CallTimeout { get; set; }
    }

    public sealed class IDGenRetryOptions
    {
        public int RetryCount { get; set; }
        public TimeSpan InitialBackOff { get; set; }
        public double BackOffMultiplier { get; set; }
        public TimeSpan MaxBackOff { get; set; }
        public int MaxJitterMs { get; set; }
    }
}
