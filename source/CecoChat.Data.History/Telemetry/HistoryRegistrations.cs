using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace CecoChat.Data.History.Telemetry;

public static class HistoryRegistrations
{
    public static TracerProviderBuilder AddHistoryInstrumentation(this TracerProviderBuilder builder)
    {
        return builder.AddSource(HistoryInstrumentation.ActivitySource.Name);
    }

    public static MeterProviderBuilder AddHistoryInstrumentation(this MeterProviderBuilder builder)
    {
        return builder.AddMeter(HistoryInstrumentation.ActivitySource.Name);
    }
}
