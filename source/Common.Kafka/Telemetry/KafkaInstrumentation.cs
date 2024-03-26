using System.Diagnostics;
using System.Reflection;

namespace Common.Kafka.Telemetry;

internal static class KafkaInstrumentation
{
    private static readonly string ActivitySourceName = "OpenTelemetry.Instrumentation.Kafka";
    private static readonly AssemblyName AssemblyName = typeof(KafkaInstrumentation).Assembly.GetName();
    private static readonly Version ActivitySourceVersion = AssemblyName.Version!;

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, ActivitySourceVersion.ToString());
    internal static readonly string ActivityName = ActivitySourceName + ".Execute";

    public static class Keys
    {
        public const string MessagingKafkaPartition = "messaging.kafka.partition";
    }

    public static class Values
    {
        public const string MessagingSystemKafka = "kafka";
        public const string MessagingDestinationKindTopic = "topic";
    }
}
