using System.Diagnostics;
using System.Reflection;

namespace CecoChat.Chats.Data.Telemetry;

internal static class ChatsInstrumentation
{
    private static readonly string ActivitySourceName = "OpenTelemetry.Instrumentation.CecoChat.ChatsDb";
    private static readonly AssemblyName AssemblyName = typeof(ChatsInstrumentation).Assembly.GetName();
    private static readonly Version ActivitySourceVersion = AssemblyName.Version!;

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, ActivitySourceVersion.ToString());
    internal static readonly string ActivityName = ActivitySourceName + ".Execute";

    public static class Operations
    {
        public const string GetChats = "GetChats";
        public const string GetChat = "GetChat";
        public const string UpdateChat = "UpdateChat";

        public const string AddPlainText = "AddPlainText";
        public const string AddFile = "AddFile";
        public const string GetHistory = "GetHistory";
        public const string SetReaction = "SetReaction";
        public const string UnsetReaction = "UnsetReaction";
    }
}
