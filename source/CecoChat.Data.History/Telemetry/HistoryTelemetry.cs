using Cassandra;
using CecoChat.Cassandra.Telemetry;

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
        _cassandraTelemetry.ExecuteStatement(
            session,
            statement,
            HistoryInstrumentation.ActivitySource,
            HistoryInstrumentation.ActivityName,
            operationName: HistoryInstrumentation.Operations.AddDataMessage,
            enrich: activity =>
            {
                activity.SetTag("cecochat.message_id", messageId);
            });
    }

    public Task<RowSet> GetHistoryAsync(ISession session, IStatement statement, long userId)
    {
        return _cassandraTelemetry.ExecuteStatementAsync(
            session,
            statement,
            HistoryInstrumentation.ActivitySource,
            HistoryInstrumentation.ActivityName,
            operationName: HistoryInstrumentation.Operations.GetHistory,
            enrich: activity =>
            {
                activity.SetTag("cecochat.user_id", userId);
            });
    }

    public void SetReaction(ISession session, IStatement statement, long reactorId)
    {
        _cassandraTelemetry.ExecuteStatement(
            session,
            statement,
            HistoryInstrumentation.ActivitySource,
            HistoryInstrumentation.ActivityName,
            operationName: HistoryInstrumentation.Operations.SetReaction,
            enrich: activity =>
            {
                activity.SetTag("cecochat.reactor_id", reactorId);
            });
    }

    public void UnsetReaction(ISession session, IStatement statement, long reactorId)
    {
        _cassandraTelemetry.ExecuteStatement(session,
            statement,
            HistoryInstrumentation.ActivitySource,
            HistoryInstrumentation.ActivityName,
            operationName: HistoryInstrumentation.Operations.UnsetReaction,
            enrich: activity =>
            {
                activity.SetTag("cecochat.reactor_id", reactorId);
            });
    }
}
