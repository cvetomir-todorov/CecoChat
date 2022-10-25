using System.Diagnostics;
using Cassandra;
using CecoChat.Otel;

namespace CecoChat.Data.State.Telemetry;

internal interface IStateTelemetry
{
    Activity StartGetChats(ISession session, long userID);

    Activity StartGetChat(ISession session, long userID, string chatID);

    Activity StartUpdateChat(ISession session, long userID, string chatID);

    void Stop(Activity activity, bool operationSuccess);
}

internal sealed class StateTelemetry : IStateTelemetry
{
    private readonly ITelemetry _telemetry;

    public StateTelemetry(ITelemetry telemetry)
    {
        _telemetry = telemetry;
    }

    public Activity StartGetChats(ISession session, long userID)
    {
        Activity activity = _telemetry.Start(
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
        Activity activity = _telemetry.Start(
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
        Activity activity = _telemetry.Start(
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
        _telemetry.Stop(activity, operationSuccess);
    }

    private static void Enrich(string operation, ISession session, Activity activity)
    {
        activity.SetTag(OtelInstrumentation.Keys.DbOperation, operation);
        activity.SetTag(OtelInstrumentation.Keys.DbSystem, OtelInstrumentation.Values.DbSystemCassandra);
        activity.SetTag(OtelInstrumentation.Keys.DbName, session.Keyspace);
        activity.SetTag(OtelInstrumentation.Keys.DbSessionName, session.SessionName);
    }
}