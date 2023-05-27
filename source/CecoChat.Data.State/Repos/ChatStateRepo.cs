using Cassandra;
using CecoChat.Contracts.State;
using CecoChat.Data.State.Telemetry;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.State.Repos;

public interface IChatStateRepo : IDisposable
{
    void Prepare();

    Task<IReadOnlyCollection<ChatState>> GetChats(long userId, DateTime newerThan);

    ChatState? GetChat(long userId, string chatId);

    void UpdateChat(long userId, ChatState chat);
}

internal sealed class ChatStateRepo : IChatStateRepo
{
    private readonly ILogger _logger;
    private readonly IStateTelemetry _stateTelemetry;
    private readonly IStateDbContext _dbContext;
    private readonly Lazy<PreparedStatement> _chatsQuery;
    private readonly Lazy<PreparedStatement> _chatQuery;
    private readonly Lazy<PreparedStatement> _updateQuery;

    public ChatStateRepo(
        ILogger<ChatStateRepo> logger,
        IStateTelemetry stateTelemetry,
        IStateDbContext dbContext)
    {
        _logger = logger;
        _stateTelemetry = stateTelemetry;
        _dbContext = dbContext;

        _chatsQuery = new Lazy<PreparedStatement>(() => _dbContext.PrepareQuery(SelectNewerChatsForUser));
        _chatQuery = new Lazy<PreparedStatement>(() => _dbContext.PrepareQuery(SelectChatForUser));
        _updateQuery = new Lazy<PreparedStatement>(() => _dbContext.PrepareQuery(UpdateChatForUser));
    }

    public void Dispose()
    {
        _stateTelemetry.Dispose();
    }

    private const string SelectNewerChatsForUser =
        "SELECT chat_id, newest_message, other_user_delivered, other_user_seen " +
        "FROM user_chats " +
        "WHERE user_id = ? AND newest_message > ? ALLOW FILTERING";
    private const string SelectChatForUser =
        "SELECT newest_message, other_user_delivered, other_user_seen " +
        "FROM user_chats " +
        "WHERE user_id = ? AND chat_id = ?;";
    private const string UpdateChatForUser =
        "INSERT into user_chats " +
        "(user_id, chat_id, newest_message, other_user_delivered, other_user_seen) " +
        "VALUES (?, ?, ?, ?, ?);";

    public void Prepare()
    {
#pragma warning disable IDE0059
#pragma warning disable IDE1006
        PreparedStatement _ = _chatsQuery.Value;
        PreparedStatement __ = _chatQuery.Value;
        PreparedStatement ___ = _updateQuery.Value;
#pragma warning restore IDE0059
#pragma warning restore IDE1006
    }

    public async Task<IReadOnlyCollection<ChatState>> GetChats(long userId, DateTime newerThan)
    {
        long newerThanSnowflake = newerThan.ToSnowflakeFloor();
        BoundStatement query = _chatsQuery.Value.Bind(userId, newerThanSnowflake);
        query.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
        query.SetIdempotence(true);

        RowSet rows = await _stateTelemetry.GetChatsAsync(_dbContext.Session, query, userId);
        List<ChatState> chats = new();

        foreach (Row row in rows)
        {
            ChatState chat = new();

            chat.ChatId = row.GetValue<string>("chat_id");
            chat.NewestMessage = row.GetValue<long>("newest_message");
            chat.OtherUserDelivered = row.GetValue<long>("other_user_delivered");
            chat.OtherUserSeen = row.GetValue<long>("other_user_seen");

            chats.Add(chat);
        }

        _logger.LogTrace("Fetched {ChatCount} chats for user {UserId} which are newer than {NewerThan}", chats.Count, userId, newerThan);
        return chats;
    }

    public ChatState? GetChat(long userId, string chatId)
    {
        BoundStatement query = _chatQuery.Value.Bind(userId, chatId);
        query.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
        query.SetIdempotence(true);

        RowSet rows = _stateTelemetry.GetChat(_dbContext.Session, query, userId, chatId);
        Row? row = rows.FirstOrDefault();
        ChatState? chat = null;
        if (row != null)
        {
            chat = new();

            chat.ChatId = chatId;
            chat.NewestMessage = row.GetValue<long>("newest_message");
            chat.OtherUserDelivered = row.GetValue<long>("other_user_delivered");
            chat.OtherUserSeen = row.GetValue<long>("other_user_seen");

            _logger.LogTrace("Fetched chat {ChatId} for user {UserId}", chatId, userId);
        }
        else
        {
            _logger.LogTrace("Failed to find chat {ChatId} for user {UserId}", chatId, userId);
        }

        return chat;
    }

    public void UpdateChat(long userId, ChatState chat)
    {
        BoundStatement query = _updateQuery.Value.Bind(userId, chat.ChatId, chat.NewestMessage, chat.OtherUserDelivered, chat.OtherUserSeen);
        query.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
        query.SetIdempotence(false);

        _stateTelemetry.UpdateChat(_dbContext.Session, query, userId, chat.ChatId);
        _logger.LogTrace("Persisted changes about chat {ChatId} for user {UserId}", chat.ChatId, userId);
    }
}
