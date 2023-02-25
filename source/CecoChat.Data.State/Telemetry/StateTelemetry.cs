using Cassandra;
using CecoChat.Cassandra.Telemetry;

namespace CecoChat.Data.State.Telemetry;

internal interface IStateTelemetry : IDisposable
{
    Task<RowSet> GetChatsAsync(ISession session, IStatement statement, long userId);

    RowSet GetChat(ISession session, IStatement statement, long userId, string chatId);

    void UpdateChat(ISession session, IStatement statement, long userId, string chatId);
}

internal sealed class StateTelemetry : IStateTelemetry
{
    private readonly ICassandraTelemetry _cassandraTelemetry;

    public StateTelemetry(ICassandraTelemetry cassandraTelemetry)
    {
        _cassandraTelemetry = cassandraTelemetry;
        _cassandraTelemetry.EnableMetrics(StateInstrumentation.ActivitySource, "statedb.query.duration");
    }

    public void Dispose()
    {
        _cassandraTelemetry.Dispose();
    }

    public Task<RowSet> GetChatsAsync(ISession session, IStatement statement, long userId)
    {
        return _cassandraTelemetry.ExecuteStatementAsync(
            session,
            statement,
            StateInstrumentation.ActivitySource,
            StateInstrumentation.ActivityName,
            operationName: StateInstrumentation.Operations.GetChats,
            enrich: activity =>
            {
                activity.SetTag("cecochat.user_id", userId);
            });
    }

    public RowSet GetChat(ISession session, IStatement statement, long userId, string chatId)
    {
        return _cassandraTelemetry.ExecuteStatement(
            session,
            statement,
            StateInstrumentation.ActivitySource,
            StateInstrumentation.ActivityName,
            operationName: StateInstrumentation.Operations.GetChat,
            enrich: activity =>
            {
                activity.SetTag("cecochat.user_id", userId);
                activity.SetTag("cecochat.chat_id", chatId);
            });
    }

    public void UpdateChat(ISession session, IStatement statement, long userId, string chatId)
    {
        _cassandraTelemetry.ExecuteStatement(
            session,
            statement,
            StateInstrumentation.ActivitySource,
            StateInstrumentation.ActivityName,
            operationName: StateInstrumentation.Operations.UpdateChat,
            enrich: activity =>
            {
                activity.SetTag("cecochat.user_id", userId);
                activity.SetTag("cecochat.chat_id", chatId);
            });
    }
}
