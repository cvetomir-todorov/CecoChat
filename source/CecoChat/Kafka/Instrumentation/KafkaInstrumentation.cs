using System;
using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.Trace;

namespace CecoChat.Kafka.Instrumentation;

public static class KafkaInstrumentationRegistrations
{
    public static TracerProviderBuilder AddKafkaInstrumentation(this TracerProviderBuilder builder)
    {
        return builder.AddSource(KafkaInstrumentation.ActivitySource.Name);
    }
}

internal static class KafkaInstrumentation
{
    private static readonly string ActivitySourceName = "OpenTelemetry.Instrumentation.CecoChatKafka";
    private static readonly AssemblyName _assemblyName = typeof(KafkaInstrumentation).Assembly.GetName();
    private static readonly Version _activitySourceVersion = _assemblyName.Version;

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, _activitySourceVersion.ToString());

    public static class Operations
    {
        public const string Production = "Kafka.Production";
        public const string Consumption = "Kafka.Consumption";
    }
}