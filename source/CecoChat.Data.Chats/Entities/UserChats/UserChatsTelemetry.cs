using Cassandra;
using CecoChat.Cassandra.Telemetry;
using CecoChat.Data.Chats.Telemetry;

namespace CecoChat.Data.Chats.Entities.UserChats;

internal interface IUserChatsTelemetry : IDisposable
{
    Task<RowSet> GetChatsAsync(ISession session, IStatement statement, long userId);

    RowSet GetChat(ISession session, IStatement statement, long userId, string chatId);

    void UpdateChat(ISession session, IStatement statement, long userId, string chatId);
}

internal sealed class UserChatsTelemetry : IUserChatsTelemetry
{
    private readonly ICassandraTelemetry _cassandraTelemetry;

    public UserChatsTelemetry(ICassandraTelemetry cassandraTelemetry)
    {
        _cassandraTelemetry = cassandraTelemetry;
        _cassandraTelemetry.EnableMetrics(ChatsInstrumentation.ActivitySource, "chats_db.query.duration");
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
            ChatsInstrumentation.ActivitySource,
            ChatsInstrumentation.ActivityName,
            operationName: ChatsInstrumentation.Operations.GetChats,
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
            ChatsInstrumentation.ActivitySource,
            ChatsInstrumentation.ActivityName,
            operationName: ChatsInstrumentation.Operations.GetChat,
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
            ChatsInstrumentation.ActivitySource,
            ChatsInstrumentation.ActivityName,
            operationName: ChatsInstrumentation.Operations.UpdateChat,
            enrich: activity =>
            {
                activity.SetTag("cecochat.user_id", userId);
                activity.SetTag("cecochat.chat_id", chatId);
            });
    }
}
