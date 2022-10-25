using System.Diagnostics;
using System.Reflection;

namespace CecoChat.Kafka.Telemetry;

internal static class KafkaInstrumentation
{
    private static readonly string ActivitySourceName = "OpenTelemetry.Instrumentation.Kafka";
    private static readonly AssemblyName _assemblyName = typeof(KafkaInstrumentation).Assembly.GetName();
    private static readonly Version _activitySourceVersion = _assemblyName.Version!;

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, _activitySourceVersion.ToString());

    public static class Operations
    {
        public const string Production = "Kafka.Production";
        public const string Consumption = "Kafka.Consumption";
    }
}