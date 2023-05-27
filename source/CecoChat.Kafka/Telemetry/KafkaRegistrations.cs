using OpenTelemetry.Trace;

namespace CecoChat.Kafka.Telemetry;

public static class KafkaRegistrations
{
    public static TracerProviderBuilder AddKafkaInstrumentation(this TracerProviderBuilder builder)
    {
        return builder.AddSource(KafkaInstrumentation.ActivitySource.Name);
    }
}
