using System;
using CecoChat.Polly;

namespace CecoChat.Client.History
{
    public sealed class HistoryOptions
    {
        public Uri Address { get; set; }

        public RetryOptions Retry { get; set; }
    }
}