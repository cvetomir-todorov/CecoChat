namespace CecoChat.Otel
{
    public static class OtelInstrumentation
    {
        public static class Keys
        {
            public const string DbSystem = "db.system";
            public const string DbName = "db.name";
            public const string DbSessionName = "db.session_name";
            public const string DbOperation = "db.operation";
            
            public const string MessagingSystem = "messaging.system";
            public const string MessagingDestination = "messaging.destination";
            public const string MessagingDestinationKind = "messaging.destination_kind";
            public const string MessagingKafkaPartition = "messaging.kafka.partition";

            public const string HeaderTraceId = "otel.trace_id";
            public const string HeaderSpanId = "otel.span_id";
            public const string HeaderTraceFlags = "otel.trace_flags";
            public const string HeaderTraceState = "otel.trace_state";
        }

        public static class Values
        {
            public const string DbSystemCassandra = "cassandra";
            public const string DbOperationBatchWrite = "batch_write";
            public const string DbOperationOneWrite = "one_write";
            public const string DbOperationOneRead = "one_read";

            public const string MessagingSystemKafka = "kafka";
            public const string MessagingDestinationKindTopic = "topic";
        }
    }
}