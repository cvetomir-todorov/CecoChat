using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace CecoChat.Data.Chats.Telemetry;

public static class ChatsRegistrations
{
    public static TracerProviderBuilder AddChatsInstrumentation(this TracerProviderBuilder builder)
    {
        return builder.AddSource(ChatsInstrumentation.ActivitySource.Name);
    }

    public static MeterProviderBuilder AddChatsInstrumentation(this MeterProviderBuilder builder)
    {
        return builder.AddMeter(ChatsInstrumentation.ActivitySource.Name);
    }
}
