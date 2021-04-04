using OpenTelemetry.Trace;

namespace CecoChat.Kafka.Instrumentation
{
    public static class KafkaInstrumentationExtensions
    {
        public static TracerProviderBuilder AddKafkaInstrumentation(this TracerProviderBuilder builder)
        {
            return builder.AddSource(KafkaInstrumentation.ActivitySource.Name);
        }
    }
}