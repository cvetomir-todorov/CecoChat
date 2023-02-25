using System.Diagnostics;
using System.Reflection;

namespace CecoChat.Kafka.Telemetry;

internal static class KafkaInstrumentation
{
    private static readonly string ActivitySourceName = "OpenTelemetry.Instrumentation.Kafka";
    private static readonly AssemblyName AssemblyName = typeof(KafkaInstrumentation).Assembly.GetName();
    private static readonly Version ActivitySourceVersion = AssemblyName.Version!;

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, ActivitySourceVersion.ToString());
    internal static readonly string ActivityName = ActivitySourceName + ".Execute";
}
