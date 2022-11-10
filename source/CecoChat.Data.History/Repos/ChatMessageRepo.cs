using Cassandra;
using CecoChat.Contracts.History;
using CecoChat.Data.History.Telemetry;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.History.Repos;

public interface IChatMessageRepo
{
    void Prepare();

    Task<IReadOnlyCollection<HistoryMessage>> GetHistory(long userId, long otherUserId, DateTime olderThan, int countLimit);

    void AddMessage(DataMessage message);

    void SetReaction(ReactionMessage message);

    void UnsetReaction(ReactionMessage message);
}

internal class ChatMessageRepo : IChatMessageRepo
{
    private readonly ILogger _logger;
    private readonly IHistoryTelemetry _historyTelemetry;
    private readonly IHistoryDbContext _dbContext;
    private readonly IDataMapper _mapper;
    private readonly Lazy<PreparedStatement> _historyQuery;
    private readonly Lazy<PreparedStatement> _messagesForChatQuery;
    private readonly Lazy<PreparedStatement> _setReactionQuery;
    private readonly Lazy<PreparedStatement> _unsetReactionQuery;

    public ChatMessageRepo(
        ILogger<ChatMessageRepo> logger,
        IHistoryTelemetry historyTelemetry,
        IHistoryDbContext dbContext,
        IDataMapper mapper)
    {
        _logger = logger;
        _historyTelemetry = historyTelemetry;
        _dbContext = dbContext;
        _mapper = mapper;

        _historyQuery = new Lazy<PreparedStatement>(() => _dbContext.PrepareQuery(SelectMessagesForChat));
        _messagesForChatQuery = new Lazy<PreparedStatement>(() => _dbContext.PrepareQuery(InsertIntoMessagesForChat));
        _setReactionQuery = new Lazy<PreparedStatement>(() => _dbContext.PrepareQuery(SetReactionCommand));
        _unsetReactionQuery = new Lazy<PreparedStatement>(() => _dbContext.PrepareQuery(UnsetReactionCommand));
    }

    private const string SelectMessagesForChat =
        "SELECT message_id, sender_id, receiver_id, type, data, reactions " +
        "FROM chat_messages " +
        "WHERE chat_id = ? AND message_id < ? ORDER BY message_id DESC LIMIT ?";
    private const string InsertIntoMessagesForChat =
        "INSERT INTO chat_messages " +
        "(chat_id, message_id, sender_id, receiver_id, type, data) " +
        "VALUES (?, ?, ?, ?, ?, ?)";
    private const string SetReactionCommand =
        "UPDATE chat_messages " +
        "SET reactions[?] = ? " +
        "WHERE chat_id = ? AND message_id = ?";
    private const string UnsetReactionCommand =
        "DELETE reactions[?] " +
        "FROM chat_messages " +
        "WHERE chat_id = ? AND message_id = ?";

    public void Prepare()
    {
        PreparedStatement _ = _historyQuery.Value;
#pragma warning disable IDE0059
        PreparedStatement __ = _messagesForChatQuery.Value;
        PreparedStatement ___ = _setReactionQuery.Value;
        PreparedStatement ____ = _unsetReactionQuery.Value;
#pragma warning restore IDE0059
    }

    public async Task<IReadOnlyCollection<HistoryMessage>> GetHistory(long userId, long otherUserId, DateTime olderThan, int countLimit)
    {
        string chatId = DataUtility.CreateChatID(userId, otherUserId);
        long olderThanSnowflake = olderThan.ToSnowflakeCeiling();
        BoundStatement query = _historyQuery.Value.Bind(chatId, olderThanSnowflake, countLimit);
        query.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
        query.SetIdempotence(true);

        RowSet rows = await _historyTelemetry.GetHistoryAsync(_dbContext.Session, query, userId);
        List<HistoryMessage> messages = new(capacity: countLimit);
        ReadRows(rows, messages);

        _logger.LogTrace("Returned {MessageCount} messages between for chat {Chat} which are older than {OlderThan}", messages.Count, chatId, olderThan);
        return messages;
    }

    private void ReadRows(RowSet rows, ICollection<HistoryMessage> messages)
    {
        foreach (Row row in rows)
        {
            HistoryMessage message = new();

            message.MessageId = row.GetValue<long>("message_id");
            message.SenderId = row.GetValue<long>("sender_id");
            message.ReceiverId = row.GetValue<long>("receiver_id");
            sbyte messageType = row.GetValue<sbyte>("type");
            message.DataType = _mapper.MapDbToHistoryDataType(messageType);
            message.Data = row.GetValue<string>("data");
            IDictionary<long, string> reactions = row.GetValue<IDictionary<long, string>>("reactions");
            if (reactions != null)
            {
                message.Reactions.Add(reactions);
            }

            messages.Add(message);
        }
    }

    public void AddMessage(DataMessage message)
    {
        sbyte dbMessageType = _mapper.MapHistoryToDbDataType(message.DataType);
        string chatId = DataUtility.CreateChatID(message.SenderId, message.ReceiverId);

        BoundStatement query = _messagesForChatQuery.Value.Bind(
            chatId, message.MessageId, message.SenderId, message.ReceiverId, dbMessageType, message.Data);
        query.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
        query.SetIdempotence(false);
        
        _historyTelemetry.AddDataMessage(_dbContext.Session, query, message.MessageId);
        _logger.LogTrace("Persisted the message {@Message}", message);
    }

    public void SetReaction(ReactionMessage message)
    {
        string chatId = DataUtility.CreateChatID(message.SenderId, message.ReceiverId);
        BoundStatement query = _setReactionQuery.Value.Bind(message.ReactorId, message.Reaction, chatId, message.MessageId);
        query.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
        query.SetIdempotence(false);

        _historyTelemetry.SetReaction(_dbContext.Session, query, message.ReactorId);
        _logger.LogTrace("User {ReactorId} reacted with {Reaction} to message {MessageId}", message.ReactorId, message.Reaction, message.MessageId);
    }

    public void UnsetReaction(ReactionMessage message)
    {
        string chatId = DataUtility.CreateChatID(message.SenderId, message.ReceiverId);
        BoundStatement query = _unsetReactionQuery.Value.Bind(message.ReactorId, chatId, message.MessageId);
        query.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
        query.SetIdempotence(false);

        _historyTelemetry.UnsetReaction(_dbContext.Session, query, message.ReactorId);
        _logger.LogTrace("User {ReactorId} removed reaction to message {MessageId}", message.ReactorId, message.MessageId);
    }
}