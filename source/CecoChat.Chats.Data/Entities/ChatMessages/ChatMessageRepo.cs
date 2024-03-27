using System.Diagnostics.CodeAnalysis;
using Cassandra;
using CecoChat.Chats.Contracts;
using CecoChat.Data;
using Common;
using Microsoft.Extensions.Logging;

namespace CecoChat.Chats.Data.Entities.ChatMessages;

public interface IChatMessageRepo : IDisposable
{
    void Prepare();

    Task<IReadOnlyCollection<HistoryMessage>> GetHistory(long userId, string chatId, DateTime olderThan, int countLimit);

    void AddPlainTextMessage(PlainTextMessage message);

    void AddFileMessage(FileMessage message);

    void SetReaction(ReactionMessage message);

    void UnsetReaction(ReactionMessage message);
}

internal sealed class ChatMessageRepo : IChatMessageRepo
{
    private readonly ILogger _logger;
    private readonly IChatMessageTelemetry _chatMessageTelemetry;
    private readonly IChatsDbContext _dbContext;
    private readonly IDataMapper _mapper;
    private PreparedStatement? _historyQuery;
    private PreparedStatement? _addPlainTextCommand;
    private PreparedStatement? _addFileCommand;
    private PreparedStatement? _setReactionCommand;
    private PreparedStatement? _unsetReactionCommand;

    public ChatMessageRepo(
        ILogger<ChatMessageRepo> logger,
        IChatMessageTelemetry chatMessageTelemetry,
        IChatsDbContext dbContext,
        IDataMapper mapper)
    {
        _logger = logger;
        _chatMessageTelemetry = chatMessageTelemetry;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public void Dispose()
    {
        _chatMessageTelemetry.Dispose();
    }

    private const string HistoryQuery =
        "SELECT message_id, sender_id, receiver_id, type, text, file, reactions " +
        "FROM chat_messages " +
        "WHERE chat_id = ? AND message_id < ? ORDER BY message_id DESC LIMIT ?";
    private const string AddPlainTextCommand =
        "INSERT INTO chat_messages " +
        "(chat_id, message_id, sender_id, receiver_id, type, text) " +
        "VALUES (?, ?, ?, ?, ?, ?)";
    private const string AddFileCommand =
        "INSERT INTO chat_messages " +
        "(chat_id, message_id, sender_id, receiver_id, type, text, file) " +
        "VALUES (?, ?, ?, ?, ?, ?, ?)";
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
        _dbContext.PrepareUdt<DbFileData>(_dbContext.Keyspace, "file_data");

        _historyQuery = _dbContext.PrepareStatement(HistoryQuery);
        _addPlainTextCommand = _dbContext.PrepareStatement(AddPlainTextCommand);
        _addFileCommand = _dbContext.PrepareStatement(AddFileCommand);
        _setReactionCommand = _dbContext.PrepareStatement(SetReactionCommand);
        _unsetReactionCommand = _dbContext.PrepareStatement(UnsetReactionCommand);
    }

    [MemberNotNull(nameof(_historyQuery), nameof(_addPlainTextCommand), nameof(_addFileCommand), nameof(_setReactionCommand), nameof(_unsetReactionCommand))]
    private void EnsurePrepared()
    {
        if (_historyQuery == null ||
            _addPlainTextCommand == null ||
            _addFileCommand == null ||
            _setReactionCommand == null ||
            _unsetReactionCommand == null)
        {
            throw new InvalidOperationException($"Repo should be prepared by calling {nameof(Prepare)}.");
        }
    }

    public async Task<IReadOnlyCollection<HistoryMessage>> GetHistory(long userId, string chatId, DateTime olderThan, int countLimit)
    {
        EnsurePrepared();

        long olderThanSnowflake = olderThan.ToSnowflakeCeiling();
        BoundStatement query = _historyQuery.Bind(chatId, olderThanSnowflake, countLimit);
        query.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
        query.SetIdempotence(true);

        RowSet rows = await _chatMessageTelemetry.GetHistoryAsync(_dbContext.Session, query, userId);
        List<HistoryMessage> messages = new(capacity: countLimit);
        ReadRows(rows, messages);

        _logger.LogTrace("Fetched {MessageCount} messages for chat {Chat} which are older than {OlderThan}", messages.Count, chatId, olderThan);
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
            message.Text = row.GetValue<string>("text");

            sbyte messageType = row.GetValue<sbyte>("type");
            message.DataType = _mapper.MapDbToContractDataType(messageType);

            DbFileData dbFile = row.GetValue<DbFileData>("file");
            if (dbFile != null)
            {
                message.File = new HistoryFileData
                {
                    Bucket = dbFile.Bucket,
                    Path = dbFile.Path
                };
            }

            IDictionary<long, string> reactions = row.GetValue<IDictionary<long, string>>("reactions");
            if (reactions != null)
            {
                message.Reactions.Add(reactions);
            }

            messages.Add(message);
        }
    }

    public void AddPlainTextMessage(PlainTextMessage message)
    {
        EnsurePrepared();

        sbyte dbMessageType = _mapper.MapContractToDbDataType(DataType.PlainText);
        string chatId = DataUtility.CreateChatId(message.SenderId, message.ReceiverId);

        BoundStatement command = _addPlainTextCommand.Bind(
            chatId, message.MessageId, message.SenderId, message.ReceiverId, dbMessageType, message.Text);
        command.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
        command.SetIdempotence(false);

        _chatMessageTelemetry.AddPlainTextMessage(_dbContext.Session, command, message.MessageId);
        _logger.LogTrace("Persisted plain text message {MessageId} for chat {ChatId}", message.MessageId, chatId);
    }

    public void AddFileMessage(FileMessage message)
    {
        EnsurePrepared();

        sbyte dbMessageType = _mapper.MapContractToDbDataType(DataType.File);
        string chatId = DataUtility.CreateChatId(message.SenderId, message.ReceiverId);

        DbFileData file = new()
        {
            Bucket = message.Bucket,
            Path = message.Path
        };

        // avoid setting the value to null which would add a tombstone
        string text = message.Text ?? string.Empty;

        BoundStatement command = _addFileCommand.Bind(
            chatId, message.MessageId, message.SenderId, message.ReceiverId, dbMessageType, text, file);
        command.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
        command.SetIdempotence(false);

        _chatMessageTelemetry.AddFileMessage(_dbContext.Session, command, message.MessageId);
        _logger.LogTrace("Persisted file message {MessageId} for chat {ChatId}", message.MessageId, chatId);
    }

    public void SetReaction(ReactionMessage message)
    {
        EnsurePrepared();

        string chatId = DataUtility.CreateChatId(message.SenderId, message.ReceiverId);
        BoundStatement command = _setReactionCommand.Bind(message.ReactorId, message.Reaction, chatId, message.MessageId);
        command.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
        command.SetIdempotence(false);

        _chatMessageTelemetry.SetReaction(_dbContext.Session, command, message.ReactorId);
        _logger.LogTrace("Persisted user {ReactorId} reaction {Reaction} to message {MessageId}", message.ReactorId, message.Reaction, message.MessageId);
    }

    public void UnsetReaction(ReactionMessage message)
    {
        EnsurePrepared();

        string chatId = DataUtility.CreateChatId(message.SenderId, message.ReceiverId);
        BoundStatement command = _unsetReactionCommand.Bind(message.ReactorId, chatId, message.MessageId);
        command.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
        command.SetIdempotence(false);

        _chatMessageTelemetry.UnsetReaction(_dbContext.Session, command, message.ReactorId);
        _logger.LogTrace("Persisted user {ReactorId} un-reaction to message {MessageId}", message.ReactorId, message.MessageId);
    }
}
