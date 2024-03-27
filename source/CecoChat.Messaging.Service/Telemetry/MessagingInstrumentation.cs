using System.Diagnostics;
using System.Reflection;

namespace CecoChat.Messaging.Service.Telemetry;

public static class MessagingInstrumentation
{
    private static readonly string ActivitySourceName = "OpenTelemetry.Instrumentation.CecoChat.Messaging";
    private static readonly AssemblyName AssemblyName = typeof(MessagingInstrumentation).Assembly.GetName();
    private static readonly Version ActivitySourceVersion = AssemblyName.Version!;

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, ActivitySourceVersion.ToString());

    public static class Metrics
    {
        public const string PlainTextsReceived = "messaging.plain_texts.received";
        public const string PlainTextsReceivedDescription = "measures how many plain text messages are received";

        public const string PlainTextsProcessed = "messaging.plain_texts.processed";
        public const string PlainTextsProcessedDescription = "measures how many plain text messages are processed";

        public const string FilesReceived = "messaging.files.received";
        public const string FilesReceivedDescription = "measures how many file messages are received";

        public const string FilesProcessed = "messaging.files.processed";
        public const string FilesProcessedDescription = "measures how many file messages are processed";

        public const string ReactionsReceived = "messaging.reactions.received";
        public const string ReactionsReceivedDescription = "measures how many reactions are received";

        public const string ReactionsProcessed = "messaging.reactions.processed";
        public const string ReactionsProcessedDescription = "measures how many reactions are processed";

        public const string UnReactionsReceived = "messaging.unreactions.received";
        public const string UnReactionsReceivedDescription = "measures how many un-reactions are received";

        public const string UnReactionsProcessed = "messaging.unreactions.processed";
        public const string UnReactionsProcessedDescription = "measures how many un-reactions are processed";

        public const string OnlineClients = "messaging.online_clients";
        public const string OnlineClientsDescription = "measures how many clients are online";
    }

    public static class Tags
    {
        public const string ServerId = "messaging.server_id";
    }
}
