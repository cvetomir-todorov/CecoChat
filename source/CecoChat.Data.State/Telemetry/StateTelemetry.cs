using System.Diagnostics;
using Cassandra;
using CecoChat.Cassandra.Telemetry;
using CecoChat.Otel;

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
        return _cassandraTelemetry.ExecuteStatementAsync(session, statement, StateInstrumentation.ActivitySource, StateInstrumentation.Operations.GetChats, activity =>
        {
            Enrich(StateInstrumentation.Operations.GetChats, session, activity);
            activity.SetTag("user.id", userId);
        });
    }

    public RowSet GetChat(ISession session, IStatement statement, long userId, string chatId)
    {
        return _cassandraTelemetry.ExecuteStatement(session, statement, StateInstrumentation.ActivitySource, StateInstrumentation.Operations.GetChat, activity =>
        {
            Enrich(StateInstrumentation.Operations.GetChat, session, activity);
            activity.SetTag("user.id", userId);
            activity.SetTag("chat.id", chatId);
        });
    }

    public void UpdateChat(ISession session, IStatement statement, long userId, string chatId)
    {
        _cassandraTelemetry.ExecuteStatement(session, statement, StateInstrumentation.ActivitySource, StateInstrumentation.Operations.UpdateChat, activity =>
        {
            Enrich(StateInstrumentation.Operations.UpdateChat, session, activity);
            activity.SetTag("user.id", userId);
            activity.SetTag("chat.id", chatId);
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
