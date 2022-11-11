using System.Diagnostics;
using System.Reflection;

namespace CecoChat.Server.Messaging.Telemetry;

public static class MessagingInstrumentation
{
    private static readonly string ActivitySourceName = "OpenTelemetry.Instrumentation.CecoChat.Messaging";
    private static readonly AssemblyName _assemblyName = typeof(MessagingInstrumentation).Assembly.GetName();
    private static readonly Version _activitySourceVersion = _assemblyName.Version!;

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, _activitySourceVersion.ToString());

    public static class Metrics
    {
        public const string MessagesReceived = "messaging.messages.received";
        public const string MessagesReceivedDescription = "measures how many messages are received";

        public const string MessagesProcessed = "messaging.messages.processed";
        public const string MessagesProcessedDescription = "measures how many messages are processed";

        public const string OnlineClients = "messaging.online_clients";
        public const string OnlineClientsDescription = "measures how many clients are online";
    }

    public static class Tags
    {
        public const string ServerId = "messaging.server_id";
    }
}
