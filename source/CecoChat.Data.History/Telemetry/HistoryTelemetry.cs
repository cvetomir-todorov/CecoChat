using System.Diagnostics;
using Cassandra;
using CecoChat.Otel;

namespace CecoChat.Data.History.Telemetry;

internal interface IHistoryTelemetry
{
    void AddDataMessage(ISession session, IStatement statement, long messageId);

    Task<RowSet> GetHistoryAsync(ISession session, IStatement statement, long userId);

    void SetReaction(ISession session, IStatement statement, long reactorId);

    void UnsetReaction(ISession session, IStatement statement, long reactorId);
}

internal sealed class HistoryTelemetry : IHistoryTelemetry
{
    private readonly ITelemetry _telemetry;

    public HistoryTelemetry(ITelemetry telemetry)
    {
        _telemetry = telemetry;
    }

    public void AddDataMessage(ISession session, IStatement statement, long messageId)
    {
        Activity activity = _telemetry.Start(
            HistoryInstrumentation.Operations.AddDataMessage,
            HistoryInstrumentation.ActivitySource,
            ActivityKind.Client,
            Activity.Current?.Context);

        if (activity.IsAllDataRequested)
        {
            Enrich(OtelInstrumentation.Values.DbOperationBatchWrite, session, activity);
            activity.SetTag("message.id", messageId);
        }

        ExecuteStatement(session, statement, activity);
    }

    public Task<RowSet> GetHistoryAsync(ISession session, IStatement statement, long userId)
    {
        Activity activity = _telemetry.Start(
            HistoryInstrumentation.Operations.GetHistory,
            HistoryInstrumentation.ActivitySource,
            ActivityKind.Client,
            Activity.Current?.Context);

        if (activity.IsAllDataRequested)
        {
            Enrich(OtelInstrumentation.Values.DbOperationOneRead, session, activity);
            activity.SetTag("user.id", userId);
        }

        return ExecuteStatementAsync(session, statement, activity);
    }

    public void SetReaction(ISession session, IStatement statement, long reactorId)
    {
        Activity activity = _telemetry.Start(
            HistoryInstrumentation.Operations.SetReaction,
            HistoryInstrumentation.ActivitySource,
            ActivityKind.Client,
            Activity.Current?.Context);

        if (activity.IsAllDataRequested)
        {
            Enrich(OtelInstrumentation.Values.DbOperationOneWrite, session, activity);
            activity.SetTag("reaction.reactor_id", reactorId);
        }

        ExecuteStatement(session, statement, activity);
    }

    public void UnsetReaction(ISession session, IStatement statement, long reactorId)
    {
        Activity activity = _telemetry.Start(
            HistoryInstrumentation.Operations.UnsetReaction,
            HistoryInstrumentation.ActivitySource,
            ActivityKind.Client,
            Activity.Current?.Context);

        if (activity.IsAllDataRequested)
        {
            Enrich(OtelInstrumentation.Values.DbOperationOneWrite, session, activity);
            activity.SetTag("reaction.reactor_id", reactorId);
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

    private void ExecuteStatement(ISession session, IStatement statement, Activity activity)
    {
        try
        {
            session.Execute(statement);
            activity.SetStatus(ActivityStatusCode.Ok);
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