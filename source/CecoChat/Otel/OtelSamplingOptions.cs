namespace CecoChat.Otel
{
    public enum OtelSamplingStrategy
    {
        AlwaysOff, AlwaysOn, Probability
    }

    public interface IOtelSamplingOptions
    {
        OtelSamplingStrategy Strategy { get; }

        double Probability { get; }
    }

    public sealed class OtelSamplingOptions : IOtelSamplingOptions
    {
        public OtelSamplingStrategy Strategy { get; set; }

        public double Probability { get; set; }
    }
}
