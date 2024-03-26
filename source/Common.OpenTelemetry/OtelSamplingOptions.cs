namespace Common.OpenTelemetry;

public enum OtelSamplingStrategy
{
    AlwaysOff, AlwaysOn, Probability
}

public sealed class OtelSamplingOptions
{
    public OtelSamplingStrategy Strategy { get; init; }

    public double Probability { get; init; }
}
