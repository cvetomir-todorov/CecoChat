using System.Diagnostics;
using Cassandra;
using CecoChat.Otel;
using CecoChat.Tracing;

namespace CecoChat.Data.State.Instrumentation;

internal interface IStateActivityUtility
{
    Activity StartGetChats(ISession session, long userID);

    Activity StartGetChat(ISession session, long userID, string chatID);

    Activity StartUpdateChat(ISession session, long userID, string chatID);

    void Stop(Activity activity, bool operationSuccess);
}

internal sealed class StateActivityUtility : IStateActivityUtility
{
    private readonly IActivityUtility _activityUtility;

    public StateActivityUtility(IActivityUtility activityUtility)
    {
        _activityUtility = activityUtility;
    }

    public Activity StartGetChats(ISession session, long userID)
    {
        Activity activity = _activityUtility.Start(
            StateInstrumentation.Operations.GetChats,
            StateInstrumentation.ActivitySource,
            ActivityKind.Client,
            Activity.Current?.Context);

        if (activity.IsAllDataRequested)
        {
            Enrich(OtelInstrumentation.Values.DbOperationOneRead, session, activity);
            activity.SetTag("user.id", userID);
        }

        return activity;
    }

    public Activity StartGetChat(ISession session, long userID, string chatID)
    {
        Activity activity = _activityUtility.Start(
            StateInstrumentation.Operations.GetChat,
            StateInstrumentation.ActivitySource,
            ActivityKind.Client,
            Activity.Current?.Context);

        if (activity.IsAllDataRequested)
        {
            Enrich(OtelInstrumentation.Values.DbOperationOneRead, session, activity);
            activity.SetTag("user.id", userID);
            activity.SetTag("chat.id", chatID);
        }

        return activity;
    }

    public Activity StartUpdateChat(ISession session, long userID, string chatID)
    {
        Activity activity = _activityUtility.Start(
            StateInstrumentation.Operations.UpdateChat,
            StateInstrumentation.ActivitySource,
            ActivityKind.Client,
            Activity.Current?.Context);

        if (activity.IsAllDataRequested)
        {
            Enrich(OtelInstrumentation.Values.DbOperationOneWrite, session, activity);
            activity.SetTag("user.id", userID);
            activity.SetTag("chat.id", chatID);
        }

        return activity;
    }

    public void Stop(Activity activity, bool operationSuccess)
    {
        _activityUtility.Stop(activity, operationSuccess);
    }

    private static void Enrich(string operation, ISession session, Activity activity)
    {
        activity.SetTag(OtelInstrumentation.Keys.DbOperation, operation);
        activity.SetTag(OtelInstrumentation.Keys.DbSystem, OtelInstrumentation.Values.DbSystemCassandra);
        activity.SetTag(OtelInstrumentation.Keys.DbName, session.Keyspace);
        activity.SetTag(OtelInstrumentation.Keys.DbSessionName, session.SessionName);
    }
}