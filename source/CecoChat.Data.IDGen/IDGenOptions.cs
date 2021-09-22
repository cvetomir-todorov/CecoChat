using System;

namespace CecoChat.Data.IDGen
{
    public interface IIDGenOptions
    {
        IIDGenGenerationOptions Generation { get; }
        IIDGenCommunicationOptions Communication { get; }
        IIDGenRetryOptions Retry { get; }
    }

    public sealed class IDGenOptions : IIDGenOptions
    {
        IIDGenGenerationOptions IIDGenOptions.Generation => Generation;
        IIDGenCommunicationOptions IIDGenOptions.Communication => Communication;
        IIDGenRetryOptions IIDGenOptions.Retry => Retry;

        public IDGenGenerationOptions Generation { get; set; }
        public IDGenCommunicationOptions Communication { get; set; }
        public IDGenRetryOptions Retry { get; set; }
    }

    public interface IIDGenGenerationOptions
    {
        int RefreshIDsCount { get; }
        int MaxConcurrentRequests { get; } 
        TimeSpan GetIDWaitInterval { get; }
        TimeSpan InvalidateIDsInterval {get;}
    }

    public sealed class IDGenGenerationOptions : IIDGenGenerationOptions
    {
        public int RefreshIDsCount { get; set; }
        public int MaxConcurrentRequests { get; set; }
        public TimeSpan GetIDWaitInterval { get; set; }
        public TimeSpan InvalidateIDsInterval { get; set; }
    }

    public interface IIDGenCommunicationOptions
    {
        Uri Address { get; }
        TimeSpan KeepAlivePingDelay { get; }
        TimeSpan KeepAlivePingTimeout { get; }
        TimeSpan CallTimeout { get; }
    }

    public sealed class IDGenCommunicationOptions : IIDGenCommunicationOptions
    {
        public Uri Address { get; set; }
        public TimeSpan KeepAlivePingDelay { get; set; }
        public TimeSpan KeepAlivePingTimeout { get; set; }
        public TimeSpan CallTimeout { get; set; }
    }

    public interface IIDGenRetryOptions
    {
        int RetryCount { get; }
        TimeSpan InitialBackOff { get; }
        double BackOffMultiplier {get;}
        TimeSpan MaxBackOff { get; }
        int MaxJitterMs { get; }
    }

    public sealed class IDGenRetryOptions : IIDGenRetryOptions
    {
        public int RetryCount { get; set; }
        public TimeSpan InitialBackOff { get; set; }
        public double BackOffMultiplier { get; set; }
        public TimeSpan MaxBackOff { get; set; }
        public int MaxJitterMs { get; set; }
    }
}
