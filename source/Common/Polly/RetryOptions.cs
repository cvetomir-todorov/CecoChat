namespace Common.Polly;

public sealed class RetryOptions
{
    public int RetryCount { get; init; }
    public TimeSpan InitialBackOff { get; init; }
    public double BackOffMultiplier { get; init; }
    public TimeSpan MaxBackOff { get; init; }
    public int MaxJitterMs { get; init; }
}
