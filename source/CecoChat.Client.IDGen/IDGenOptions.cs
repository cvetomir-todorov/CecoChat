using System;
using CecoChat.Polly;

namespace CecoChat.Client.IDGen
{
    public sealed class IDGenOptions
    {
        public IDGenGenerationOptions Generation { get; set; }
        public IDGenCommunicationOptions Communication { get; set; }
        public RetryOptions Retry { get; set; }
    }

    public sealed class IDGenGenerationOptions
    {
        public long OriginatorID { get; set; }
        public int RefreshIDsCount { get; set; }
        public TimeSpan RefreshIDsInterval { get; set; }
        public TimeSpan GetIDWaitInterval { get; set; }
    }

    public sealed class IDGenCommunicationOptions
    {
        public Uri Address { get; set; }
        public TimeSpan KeepAlivePingDelay { get; set; }
        public TimeSpan KeepAlivePingTimeout { get; set; }
        public TimeSpan CallTimeout { get; set; }
    }
}
