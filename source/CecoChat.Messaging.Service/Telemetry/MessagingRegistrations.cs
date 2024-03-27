using OpenTelemetry.Metrics;

namespace CecoChat.Messaging.Service.Telemetry;

public static class MessagingRegistrations
{
    public static MeterProviderBuilder AddMessagingInstrumentation(this MeterProviderBuilder builder)
    {
        return builder.AddMeter(MessagingInstrumentation.ActivitySource.Name);
    }
}
