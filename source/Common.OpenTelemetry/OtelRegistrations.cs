using OpenTelemetry.Trace;

namespace Common.OpenTelemetry;

public static class OtelRegistrations
{
    public static TracerProviderBuilder ConfigureSampling(this TracerProviderBuilder tracing, OtelSamplingOptions samplingOptions)
    {
        Sampler sampler;
        switch (samplingOptions.Strategy)
        {
            case OtelSamplingStrategy.AlwaysOff:
                sampler = new AlwaysOffSampler();
                break;
            case OtelSamplingStrategy.AlwaysOn:
                sampler = new AlwaysOnSampler();
                break;
            case OtelSamplingStrategy.Probability:
                sampler = new TraceIdRatioBasedSampler(samplingOptions.Probability);
                break;
            default:
                throw new EnumValueNotSupportedException(samplingOptions.Strategy);
        }

        return tracing.SetSampler(sampler);
    }
}
