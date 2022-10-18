using System;
using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.Trace;

namespace CecoChat.Data.History.Instrumentation
{
    public static class HistoryInstrumentationRegistrations
    {
        public static TracerProviderBuilder AddHistoryInstrumentation(this TracerProviderBuilder builder)
        {
            return builder.AddSource(HistoryInstrumentation.ActivitySource.Name);
        }
    }

    internal static class HistoryInstrumentation
    {
        private static readonly string ActivitySourceName = "OpenTelemetry.Instrumentation.CecoChatHistoryDB";
        private static readonly AssemblyName _assemblyName = typeof(HistoryInstrumentation).Assembly.GetName();
        private static readonly Version _activitySourceVersion = _assemblyName.Version;

        internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, _activitySourceVersion.ToString());

        public static class Operations
        {
            public const string AddDataMessage = "HistoryDB.AddDataMessage";
            public const string GetHistory = "HistoryDB.GetHistory";
            public const string SetReaction = "HistoryDB.SetReaction";
            public const string UnsetReaction = "HistoryDB.UnsetReaction";
        }
    }
}
