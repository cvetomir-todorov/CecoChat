using System.Diagnostics;
using Cassandra;
using CecoChat.Otel;

namespace CecoChat.Data.State.Telemetry;

internal interface IStateTelemetry
{
    Task<RowSet> GetChatsAsync(ISession session, IStatement statement, long userId);

    RowSet GetChat(ISession session, IStatement statement, long userId, string chatId);

    void UpdateChat(ISession session, IStatement statement, long userId, string chatId);
}

internal sealed class StateTelemetry : IStateTelemetry
{
    private readonly ITelemetry _telemetry;

    public StateTelemetry(ITelemetry telemetry)
    {
        _telemetry = telemetry;
    }

    public Task<RowSet> GetChatsAsync(ISession session, IStatement statement, long userId)
    {
        Activity activity = _telemetry.Start(
            StateInstrumentation.Operations.GetChats,
            StateInstrumentation.ActivitySource,
            ActivityKind.Client,
            Activity.Current?.Context);

        if (activity.IsAllDataRequested)
        {
            Enrich(OtelInstrumentation.Values.DbOperationOneRead, session, activity);
            activity.SetTag("user.id", userId);
        }

        return ExecuteStatementAsync(session, statement, activity);
    }

    public RowSet GetChat(ISession session, IStatement statement, long userId, string chatId)
    {
        Activity activity = _telemetry.Start(
            StateInstrumentation.Operations.GetChat,
            StateInstrumentation.ActivitySource,
            ActivityKind.Client,
            Activity.Current?.Context);

        if (activity.IsAllDataRequested)
        {
            Enrich(OtelInstrumentation.Values.DbOperationOneRead, session, activity);
            activity.SetTag("user.id", userId);
            activity.SetTag("chat.id", chatId);
        }

        return ExecuteStatement(session, statement, activity);
    }

    public void UpdateChat(ISession session, IStatement statement, long userId, string chatId)
    {
        Activity activity = _telemetry.Start(
            StateInstrumentation.Operations.UpdateChat,
            StateInstrumentation.ActivitySource,
            ActivityKind.Client,
            Activity.Current?.Context);

        if (activity.IsAllDataRequested)
        {
            Enrich(OtelInstrumentation.Values.DbOperationOneWrite, session, activity);
            activity.SetTag("user.id", userId);
            activity.SetTag("chat.id", chatId);
        }

        ExecuteStatement(session, statement, activity);
    }

    private static void Enrich(string operation, ISession session, Activity activity)
    {
        activity.SetTag(OtelInstrumentation.Keys.DbOperation, operation);
        activity.SetTag(OtelInstrumentation.Keys.DbSystem, OtelInstrumentation.Values.DbSystemCassandra);
        activity.SetTag(OtelInstrumentation.Keys.DbName, session.Keyspace);
        activity.SetTag(OtelInstrumentation.Keys.DbSessionName, session.SessionName);
    }

    private RowSet ExecuteStatement(ISession session, IStatement statement, Activity activity)
    {
        try
        {
            RowSet rows = session.Execute(statement);
            activity.SetStatus(ActivityStatusCode.Ok);
            return rows;
        }
        catch
        {
            activity.SetStatus(ActivityStatusCode.Error);
            throw;
        }
        finally
        {
            activity.Stop();
        }
    }

    private async Task<RowSet> ExecuteStatementAsync(ISession session, IStatement statement, Activity activity)
    {
        try
        {
            RowSet rowSet = await session.ExecuteAsync(statement);
            activity.SetStatus(ActivityStatusCode.Ok);
            return rowSet;
        }
        catch
        {
            activity.SetStatus(ActivityStatusCode.Error);
            throw;
        }
        finally
        {
            activity.Stop();
        }
    }
}
