using System;
using System.Diagnostics;
using System.Reflection;

namespace CecoChat.Data.History.Instrumentation
{
    internal static class HistoryInstrumentation
    {
        private static readonly string ActivitySourceName = "OpenTelemetry.Instrumentation.CecoChatHistoryDB";
        private static readonly AssemblyName _assemblyName = typeof(HistoryInstrumentation).Assembly.GetName();
        private static readonly Version _activitySourceVersion = _assemblyName.Version;

        internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, _activitySourceVersion.ToString());

        public static class Operations
        {
            public const string NewDialogMessage = "HistoryDB.NewDialogMessage";
            public const string GetUserHistory = "HistoryDB.GetUserHistory";
            public const string GetDialogHistory = "HistoryDB.GetDialogHistory";
            public const string AddReaction = "HistoryDB.AddReaction";
            public const string RemoveReaction = "HistoryDB.RemoveReaction";
        }

        public static class Keys
        {
            public const string DbSystem = "db.system";
            public const string DbName = "db.name";
            public const string DbSessionName = "db.session_name";
            public const string DbOperation = "db.operation";
        }

        public static class Values
        {
            public const string DbSystemCassandra = "cassandra";
            public const string DbOperationBatchWrite = "batch_write";
            public const string DbOperationOneWrite = "one_write";
            public const string DbOperationOneRead = "one_read";
        }
    }
}
