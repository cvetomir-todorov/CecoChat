using System;
using System.Globalization;
using OpenTelemetry.Trace;

namespace CecoChat.Otel;

/// <summary>
/// A custom sampler based on
/// https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry/Trace/TraceIdRatioBasedSampler.cs
/// </summary>
public sealed class CustomSampler : Sampler
{
    private readonly long _idUpperBound;

    public CustomSampler(double probability)
    {
        if (probability < 0.0d || probability > 1.0d)
        {
            throw new ArgumentOutOfRangeException(nameof(probability), probability, "Probability must be in range [0.0, 1.0]");
        }

        // The expected description is like CustomSampler{Probability=0.000100}
        Description = $"CustomSampler{{Probability={probability.ToString("F6", CultureInfo.InvariantCulture)}}}";

        // Special case the limits, to avoid any possible issues with lack of precision across
        // double/long boundaries. For probability == 0.0, we use Long.MIN_VALUE as this guarantees
        // that we will never sample a trace, even in the case where the id == Long.MIN_VALUE, since
        // Math.Abs(Long.MIN_VALUE) == Long.MIN_VALUE.
        if (probability == 0.0d)
        {
            _idUpperBound = long.MinValue;
        }
        else if (probability >= 1.0d)
        {
            _idUpperBound = long.MaxValue;
        }
        else
        {
            _idUpperBound = (long)(probability * long.MaxValue);
        }
    }

    /// <inheritdoc />
    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        // Always sample if we are within probability range. This is true even for child activities (that
        // may have had a different sampling decision made) to allow for different sampling policies,
        // and dynamic increases to sampling probabilities for debugging purposes.
        // Note use of '<' for comparison. This ensures that we never sample for probability == 0.0,
        // while allowing for a (very) small chance of *not* sampling if the id == Long.MAX_VALUE.
        // This is considered a reasonable trade-off for the simplicity/performance requirements (this
        // code is executed in-line for every Activity creation).
        Span<byte> traceIdBytes = stackalloc byte[16];
        samplingParameters.TraceId.CopyTo(traceIdBytes);
        bool isSampled = Math.Abs(GetLowerLong(traceIdBytes)) < _idUpperBound;

        return new SamplingResult(isSampled);
    }

    private static long GetLowerLong(ReadOnlySpan<byte> bytes)
    {
        long result = 0;
        for (int i = 0; i < 8; i++)
        {
            result <<= 8;
#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand
            result |= bytes[i] & 0xff;
#pragma warning restore CS0675 // Bitwise-or operator used on a sign-extended operand
        }

        return result;
    }
}