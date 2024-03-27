using Cassandra;
using CecoChat.Chats.Data.Telemetry;
using Common.Cassandra.Telemetry;

namespace CecoChat.Chats.Data.Entities.ChatMessages;

internal interface IChatMessageTelemetry : IDisposable
{
    void AddPlainTextMessage(ISession session, IStatement statement, long messageId);

    void AddFileMessage(ISession session, IStatement statement, long messageId);

    Task<RowSet> GetHistoryAsync(ISession session, IStatement statement, long userId);

    void SetReaction(ISession session, IStatement statement, long reactorId);

    void UnsetReaction(ISession session, IStatement statement, long reactorId);
}

internal sealed class ChatMessageTelemetry : IChatMessageTelemetry
{
    private readonly ICassandraTelemetry _cassandraTelemetry;

    public ChatMessageTelemetry(ICassandraTelemetry cassandraTelemetry)
    {
        _cassandraTelemetry = cassandraTelemetry;
        _cassandraTelemetry.EnableMetrics(ChatsInstrumentation.ActivitySource, "chats_db.query.duration");
    }

    public void Dispose()
    {
        _cassandraTelemetry.Dispose();
    }

    public void AddPlainTextMessage(ISession session, IStatement statement, long messageId)
    {
        _cassandraTelemetry.ExecuteStatement(
            session,
            statement,
            ChatsInstrumentation.ActivitySource,
            ChatsInstrumentation.ActivityName,
            operationName: ChatsInstrumentation.Operations.AddPlainText,
            enrich: activity =>
            {
                activity.SetTag("cecochat.message_id", messageId);
            });
    }

    public void AddFileMessage(ISession session, IStatement statement, long messageId)
    {
        _cassandraTelemetry.ExecuteStatement(
            session,
            statement,
            ChatsInstrumentation.ActivitySource,
            ChatsInstrumentation.ActivityName,
            operationName: ChatsInstrumentation.Operations.AddFile,
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
            ChatsInstrumentation.ActivitySource,
            ChatsInstrumentation.ActivityName,
            operationName: ChatsInstrumentation.Operations.GetHistory,
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
            ChatsInstrumentation.ActivitySource,
            ChatsInstrumentation.ActivityName,
            operationName: ChatsInstrumentation.Operations.SetReaction,
            enrich: activity =>
            {
                activity.SetTag("cecochat.reactor_id", reactorId);
            });
    }

    public void UnsetReaction(ISession session, IStatement statement, long reactorId)
    {
        _cassandraTelemetry.ExecuteStatement(session,
            statement,
            ChatsInstrumentation.ActivitySource,
            ChatsInstrumentation.ActivityName,
            operationName: ChatsInstrumentation.Operations.UnsetReaction,
            enrich: activity =>
            {
                activity.SetTag("cecochat.reactor_id", reactorId);
            });
    }
}
