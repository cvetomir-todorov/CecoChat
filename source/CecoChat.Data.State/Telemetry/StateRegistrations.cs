using OpenTelemetry.Trace;

namespace CecoChat.Data.State.Telemetry;

public static class StateRegistrations
{
    public static TracerProviderBuilder AddStateInstrumentation(this TracerProviderBuilder builder)
    {
        return builder.AddSource(StateInstrumentation.ActivitySource.Name);
    }
}