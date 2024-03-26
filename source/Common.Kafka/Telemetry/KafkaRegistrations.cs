using OpenTelemetry.Trace;

namespace Common.Kafka.Telemetry;

public static class KafkaRegistrations
{
    public static TracerProviderBuilder AddKafkaInstrumentation(this TracerProviderBuilder builder)
    {
        return builder.AddSource(KafkaInstrumentation.ActivitySource.Name);
    }
}
