using System;
using System.Diagnostics;
using Cassandra;
using CecoChat.Tracing;

namespace CecoChat.Data.History.Instrumentation
{
    public interface IHistoryActivityUtility
    {
        Activity StartNewDialogMessage(ISession session, Guid messageID);

        Activity StartGetHistory(string name, ISession session, long userID);

        void Stop(Activity activity, bool operationSuccess);
    }

    public sealed class HistoryActivityUtility : IHistoryActivityUtility
    {
        private readonly IActivityUtility _activityUtility;

        public HistoryActivityUtility(IActivityUtility activityUtility)
        {
            _activityUtility = activityUtility;
        }

        public Activity StartNewDialogMessage(ISession session, Guid messageID)
        {
            Activity activity = _activityUtility.Start(
                HistoryInstrumentation.Operations.HistoryNewDialogMessage,
                HistoryInstrumentation.ActivitySource,
                ActivityKind.Client,
                Activity.Current?.Context);

            if (activity.IsAllDataRequested)
            {
                Enrich(session, activity);
                activity.SetTag(HistoryInstrumentation.Keys.DbOperation, HistoryInstrumentation.Values.DbOperationBatchWrite);
                activity.SetTag("message.id", messageID);
            }

            return activity;
        }

        public Activity StartGetHistory(string name, ISession session, long userID)
        {
            Activity activity = _activityUtility.Start(
                name,
                HistoryInstrumentation.ActivitySource,
                ActivityKind.Client,
                Activity.Current?.Context);

            if (activity.IsAllDataRequested)
            {
                Enrich(session, activity);
                activity.SetTag(HistoryInstrumentation.Keys.DbOperation, HistoryInstrumentation.Values.DbOperationOneRead);
                activity.SetTag("user.id", userID);
            }

            return activity;
        }

        public void Stop(Activity activity, bool operationSuccess)
        {
            _activityUtility.Stop(activity, operationSuccess);
        }

        private void Enrich(ISession session, Activity activity)
        {
            activity.SetTag(HistoryInstrumentation.Keys.DbSystem, HistoryInstrumentation.Values.DbSystemCassandra);
            activity.SetTag(HistoryInstrumentation.Keys.DbName, session.Keyspace);
            activity.SetTag(HistoryInstrumentation.Keys.DbSessionName, session.SessionName);
        }
    }
}
