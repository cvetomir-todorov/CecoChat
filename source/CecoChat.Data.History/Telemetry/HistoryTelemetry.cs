using System.Diagnostics;
using Cassandra;
using CecoChat.Cassandra.Telemetry;
using CecoChat.Otel;

namespace CecoChat.Data.History.Telemetry;

internal interface IHistoryTelemetry : IDisposable
{
    void AddDataMessage(ISession session, IStatement statement, long messageId);

    Task<RowSet> GetHistoryAsync(ISession session, IStatement statement, long userId);

    void SetReaction(ISession session, IStatement statement, long reactorId);

    void UnsetReaction(ISession session, IStatement statement, long reactorId);
}

internal sealed class HistoryTelemetry : IHistoryTelemetry
{
    private readonly ICassandraTelemetry _cassandraTelemetry;

    public HistoryTelemetry(ICassandraTelemetry cassandraTelemetry)
    {
        _cassandraTelemetry = cassandraTelemetry;
        _cassandraTelemetry.EnableMetrics(HistoryInstrumentation.ActivitySource, "historydb.query.duration");
    }

    public void Dispose()
    {
        _cassandraTelemetry.Dispose();
    }

    public void AddDataMessage(ISession session, IStatement statement, long messageId)
    {
        _cassandraTelemetry.ExecuteStatement(session, statement, HistoryInstrumentation.ActivitySource, HistoryInstrumentation.Operations.AddDataMessage, activity =>
        {
            Enrich(HistoryInstrumentation.Operations.AddDataMessage, session, activity);
            activity.SetTag("message.id", messageId);
        });
    }

    public Task<RowSet> GetHistoryAsync(ISession session, IStatement statement, long userId)
    {
        return _cassandraTelemetry.ExecuteStatementAsync(session, statement, HistoryInstrumentation.ActivitySource, HistoryInstrumentation.Operations.GetHistory, activity =>
        {
            Enrich(HistoryInstrumentation.Operations.GetHistory, session, activity);
            activity.SetTag("user.id", userId);
        });
    }

    public void SetReaction(ISession session, IStatement statement, long reactorId)
    {
        _cassandraTelemetry.ExecuteStatement(session, statement, HistoryInstrumentation.ActivitySource, HistoryInstrumentation.Operations.SetReaction, activity =>
        {
            Enrich(HistoryInstrumentation.Operations.SetReaction, session, activity);
            activity.SetTag("reaction.reactor_id", reactorId);
        });
    }

    public void UnsetReaction(ISession session, IStatement statement, long reactorId)
    {
        _cassandraTelemetry.ExecuteStatement(session, statement, HistoryInstrumentation.ActivitySource, HistoryInstrumentation.Operations.UnsetReaction, activity =>
        {
            Enrich(HistoryInstrumentation.Operations.UnsetReaction, session, activity);
            activity.SetTag("reaction.reactor_id", reactorId);
        });
    }

    private static void Enrich(string operation, ISession session, Activity activity)
    {
        activity.SetTag(OtelInstrumentation.Keys.DbOperation, operation);
        activity.SetTag(OtelInstrumentation.Keys.DbSystem, OtelInstrumentation.Values.DbSystemCassandra);
        activity.SetTag(OtelInstrumentation.Keys.DbName, session.Keyspace);
        activity.SetTag(OtelInstrumentation.Keys.DbSessionName, session.SessionName);
    }
}