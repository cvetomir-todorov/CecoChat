namespace Common.OpenTelemetry;

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

        public const string RpcSystem = "rpc.system";
        public const string RpcService = "rpc.service";
        public const string RpcMethod = "rpc.method";

        public const string HeaderTraceId = "otel.trace_id";
        public const string HeaderSpanId = "otel.span_id";
        public const string HeaderTraceFlags = "otel.trace_flags";
        public const string HeaderTraceState = "otel.trace_state";
    }
}
