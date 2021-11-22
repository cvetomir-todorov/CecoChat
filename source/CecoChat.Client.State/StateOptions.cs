using System;
using CecoChat.Polly;

namespace CecoChat.Client.State
{
    public sealed class StateOptions
    {
        public Uri Address { get; set; }

        public RetryOptions Retry { get; set; }
    }
}