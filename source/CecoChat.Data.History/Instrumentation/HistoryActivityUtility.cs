using System.Diagnostics;
using Cassandra;
using CecoChat.Tracing;

namespace CecoChat.Data.History.Instrumentation
{
    internal interface IHistoryActivityUtility
    {
        Activity StartNewDialogMessage(ISession session, long messageID);

        Activity StartGetHistory(string name, ISession session, long userID);

        Activity StartAddReaction(ISession session, long reactorID);

        Activity StartRemoveReaction(ISession session, long reactorID);

        void Stop(Activity activity, bool operationSuccess);
    }

    internal sealed class HistoryActivityUtility : IHistoryActivityUtility
    {
        private readonly IActivityUtility _activityUtility;

        public HistoryActivityUtility(IActivityUtility activityUtility)
        {
            _activityUtility = activityUtility;
        }

        public Activity StartNewDialogMessage(ISession session, long messageID)
        {
            Activity activity = _activityUtility.Start(
                HistoryInstrumentation.Operations.NewDialogMessage,
                HistoryInstrumentation.ActivitySource,
                ActivityKind.Client,
                Activity.Current?.Context);

            if (activity.IsAllDataRequested)
            {
                Enrich(HistoryInstrumentation.Values.DbOperationBatchWrite, session, activity);
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
                Enrich(HistoryInstrumentation.Values.DbOperationOneRead, session, activity);
                activity.SetTag("user.id", userID);
            }

            return activity;
        }

        public Activity StartAddReaction(ISession session, long reactorID)
        {
            Activity activity = _activityUtility.Start(
                HistoryInstrumentation.Operations.AddReaction,
                HistoryInstrumentation.ActivitySource,
                ActivityKind.Client,
                Activity.Current?.Context);

            if (activity.IsAllDataRequested)
            {
                Enrich(HistoryInstrumentation.Values.DbOperationOneWrite, session, activity);
                activity.SetTag("reaction.reactor_id", reactorID);
            }

            return activity;
        }

        public Activity StartRemoveReaction(ISession session, long reactorID)
        {
            Activity activity = _activityUtility.Start(
                HistoryInstrumentation.Operations.RemoveReaction,
                HistoryInstrumentation.ActivitySource,
                ActivityKind.Client,
                Activity.Current?.Context);

            if (activity.IsAllDataRequested)
            {
                Enrich(HistoryInstrumentation.Values.DbOperationOneWrite, session, activity);
                activity.SetTag("reaction.reactor_id", reactorID);
            }

            return activity;
        }

        public void Stop(Activity activity, bool operationSuccess)
        {
            _activityUtility.Stop(activity, operationSuccess);
        }

        private static void Enrich(string operation, ISession session, Activity activity)
        {
            activity.SetTag(HistoryInstrumentation.Keys.DbOperation, operation);
            activity.SetTag(HistoryInstrumentation.Keys.DbSystem, HistoryInstrumentation.Values.DbSystemCassandra);
            activity.SetTag(HistoryInstrumentation.Keys.DbName, session.Keyspace);
            activity.SetTag(HistoryInstrumentation.Keys.DbSessionName, session.SessionName);
        }
    }
}
