using OpenTelemetry.Metrics;

namespace CecoChat.Server.Messaging.Telemetry;

public static class MessagingRegistrations
{
    public static MeterProviderBuilder AddMessagingInstrumentation(this MeterProviderBuilder builder)
    {
        return builder.AddMeter(MessagingInstrumentation.ActivitySource.Name);
    }
}
