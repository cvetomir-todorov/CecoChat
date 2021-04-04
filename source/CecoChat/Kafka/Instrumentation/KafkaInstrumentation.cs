using System;
using System.Diagnostics;
using System.Reflection;

namespace CecoChat.Kafka.Instrumentation
{
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

        public static class Keys
        {
            public const string TagMessagingSystem = "messaging.system";
            public const string TagMessagingDestination = "messaging.destination";
            public const string TagMessagingDestinationKind = "messaging.destination_kind";
            public const string TagMessagingKafkaPartition = "messaging.kafka.partition";

            public const string HeaderTraceId = "otel.trace_id";
            public const string HeaderSpanId = "otel.span_id";
            public const string HeaderTraceFlags = "otel.trace_flags";
            public const string HeaderTraceState = "otel.trace_state";
        }

        public static class Values
        {
            public const string TagMessagingSystemKafka = "kafka";
            public const string TagMessagingDestinationKindTopic = "topic";
        }
    }
}
