using System.Diagnostics;
using System.Reflection;

namespace CecoChat.Data.State.Telemetry;

internal static class StateInstrumentation
{
    private static readonly string ActivitySourceName = "OpenTelemetry.Instrumentation.CecoChat.StateDB";
    private static readonly AssemblyName AssemblyName = typeof(StateInstrumentation).Assembly.GetName();
    private static readonly Version ActivitySourceVersion = AssemblyName.Version!;

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, ActivitySourceVersion.ToString());

    public static class Operations
    {
        public const string GetChats = "StateDB.GetChats";
        public const string GetChat = "StateDB.GetChat";
        public const string UpdateChat = "StateDB.UpdateChat";
    }
}
