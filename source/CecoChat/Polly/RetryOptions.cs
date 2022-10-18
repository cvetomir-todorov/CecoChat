using System;

namespace CecoChat.Polly;

public sealed class RetryOptions
{
    public int RetryCount { get; set; }
    public TimeSpan InitialBackOff { get; set; }
    public double BackOffMultiplier { get; set; }
    public TimeSpan MaxBackOff { get; set; }
    public int MaxJitterMs { get; set; }
}