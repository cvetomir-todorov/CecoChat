using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Common.AspNet.SignalR.Telemetry;

public static class SignalRRegistrations
{
    public static TracerProviderBuilder AddSignalRInstrumentation(this TracerProviderBuilder builder)
    {
        return builder.AddSource(SignalRInstrumentation.ActivitySource.Name);
    }

    public static MeterProviderBuilder AddSignalRInstrumentation(this MeterProviderBuilder builder)
    {
        return builder.AddMeter(SignalRInstrumentation.ActivitySource.Name);
    }
}
