using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.Trace;

namespace CecoChat.Data.State.Instrumentation;

public static class StateInstrumentationRegistrations
{
    public static TracerProviderBuilder AddStateInstrumentation(this TracerProviderBuilder builder)
    {
        return builder.AddSource(StateInstrumentation.ActivitySource.Name);
    }
}

internal static class StateInstrumentation
{
    private static readonly string ActivitySourceName = "OpenTelemetry.Instrumentation.CecoChatStateDB";
    private static readonly AssemblyName _assemblyName = typeof(StateInstrumentation).Assembly.GetName();
    private static readonly Version _activitySourceVersion = _assemblyName.Version;

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, _activitySourceVersion.ToString());

    public static class Operations
    {
        public const string GetChats = "StateDB.GetChats";
        public const string GetChat = "StateDB.GetChat";
        public const string UpdateChat = "StateDB.UpdateChat";
    }
}