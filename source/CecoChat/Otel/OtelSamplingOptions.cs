namespace CecoChat.Otel;

public enum OtelSamplingStrategy
{
    AlwaysOff, AlwaysOn, Probability
}

public sealed class OtelSamplingOptions
{
    public OtelSamplingStrategy Strategy { get; set; }

    public double Probability { get; set; }
}