using System.Diagnostics.CodeAnalysis;
using Cassandra;
using CecoChat.Chats.Contracts;
using CecoChat.Data;
using Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Chats.Data.Entities.UserChats;

public interface IUserChatsRepo : IDisposable
{
    void Prepare();

    Task<IReadOnlyCollection<ChatState>> GetUserChats(long userId, DateTime newerThan);

    ChatState? GetUserChat(long userId, string chatId);

    void UpdateUserChat(long userId, ChatState chat);

    /// <summary>
    /// Deletes user chats for both users.
    /// Used only in testing to clean up the test data.
    /// </summary>
    void DeleteUserChat(long userId1, long userId2);
}

internal sealed class UserChatsRepo : IUserChatsRepo
{
    private readonly ILogger _logger;
    private readonly IUserChatsTelemetry _userChatsTelemetry;
    private readonly IChatsDbContext _dbContext;
    private readonly UserChatsOperationOptions _operationOptions;
    private PreparedStatement? _chatsQuery;
    private PreparedStatement? _chatQuery;
    private PreparedStatement? _updateChatCommand;
    private PreparedStatement? _deleteChatCommand;

    public UserChatsRepo(
        ILogger<UserChatsRepo> logger,
        IUserChatsTelemetry userChatsTelemetry,
        IChatsDbContext dbContext,
        IOptions<UserChatsOperationOptions> operationOptions)
    {
        _logger = logger;
        _userChatsTelemetry = userChatsTelemetry;
        _dbContext = dbContext;
        _operationOptions = operationOptions.Value;
    }

    public void Dispose()
    {
        _userChatsTelemetry.Dispose();
    }

    private const string ChatsQuery =
        "SELECT other_user_id, chat_id, newest_message, other_user_delivered, other_user_seen " +
        "FROM user_chats " +
        "WHERE user_id = ? AND newest_message > ? ALLOW FILTERING";
    private const string ChatQuery =
        "SELECT other_user_id, newest_message, other_user_delivered, other_user_seen " +
        "FROM user_chats " +
        "WHERE user_id = ? AND chat_id = ?;";
    private const string UpdateChatCommand =
        "INSERT into user_chats " +
        "(user_id, other_user_id, chat_id, newest_message, other_user_delivered, other_user_seen) " +
        "VALUES (?, ?, ?, ?, ?, ?);";
    private const string DeleteChatCommand =
        "DELETE FROM user_chats " +
        "WHERE user_id = ? AND chat_id = ?;";

    public void Prepare()
    {
        _chatsQuery = _dbContext.PrepareStatement(ChatsQuery);
        _chatQuery = _dbContext.PrepareStatement(ChatQuery);
        _updateChatCommand = _dbContext.PrepareStatement(UpdateChatCommand);
        _deleteChatCommand = _dbContext.PrepareStatement(DeleteChatCommand);
    }

    [MemberNotNull(
        nameof(_chatsQuery),
        nameof(_chatQuery),
        nameof(_updateChatCommand),
        nameof(_deleteChatCommand))]
    private void EnsurePrepared()
    {
        if (_chatsQuery == null ||
            _chatQuery == null ||
            _updateChatCommand == null ||
            _deleteChatCommand == null)
        {
            throw new InvalidOperationException($"Repo should be prepared by calling {nameof(Prepare)}.");
        }
    }

    public async Task<IReadOnlyCollection<ChatState>> GetUserChats(long userId, DateTime newerThan)
    {
        EnsurePrepared();

        long newerThanSnowflake = newerThan.ToSnowflakeFloor();
        BoundStatement query = _chatsQuery.Bind(userId, newerThanSnowflake);
        query.SetConsistencyLevel(_operationOptions.GetUserChats.ConsistencyLevel);
        query.SetIdempotence(true);

        RowSet rows = await _userChatsTelemetry.GetChatsAsync(_dbContext.Session, query, userId);
        List<ChatState> chats = new();

        foreach (Row row in rows)
        {
            ChatState chat = new();

            chat.OtherUserId = row.GetValue<long>("other_user_id");
            chat.ChatId = row.GetValue<string>("chat_id");
            chat.NewestMessage = row.GetValue<long>("newest_message");
            chat.OtherUserDelivered = row.GetValue<long>("other_user_delivered");
            chat.OtherUserSeen = row.GetValue<long>("other_user_seen");

            chats.Add(chat);
        }

        _logger.LogTrace("Fetched {ChatCount} chats for user {UserId} which are newer than {NewerThan}", chats.Count, userId, newerThan);
        return chats;
    }

    public ChatState? GetUserChat(long userId, string chatId)
    {
        EnsurePrepared();

        BoundStatement query = _chatQuery.Bind(userId, chatId);
        query.SetConsistencyLevel(_operationOptions.GetUserChat.ConsistencyLevel);
        query.SetIdempotence(true);

        RowSet rows = _userChatsTelemetry.GetChat(_dbContext.Session, query, userId, chatId);
        Row? row = rows.FirstOrDefault();
        ChatState? chat = null;
        if (row != null)
        {
            chat = new();

            chat.OtherUserId = row.GetValue<long>("other_user_id");
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

    public void UpdateUserChat(long userId, ChatState chat)
    {
        EnsurePrepared();

        BoundStatement command = _updateChatCommand.Bind(userId, chat.OtherUserId, chat.ChatId, chat.NewestMessage, chat.OtherUserDelivered, chat.OtherUserSeen);
        command.SetConsistencyLevel(_operationOptions.UpdateUserChat.ConsistencyLevel);
        command.SetIdempotence(false);

        _userChatsTelemetry.UpdateChat(_dbContext.Session, command, userId, chat.ChatId);
        _logger.LogTrace("Persisted changes about chat {ChatId} for user {UserId}", chat.ChatId, userId);
    }

    public void DeleteUserChat(long userId1, long userId2)
    {
        EnsurePrepared();

        string chatId = DataUtility.CreateChatId(userId1, userId2);

        BoundStatement user1Command = _deleteChatCommand.Bind(userId1, chatId);
        user1Command.SetConsistencyLevel(_operationOptions.DeleteUserChat.ConsistencyLevel);
        user1Command.SetIdempotence(true);

        BoundStatement user2Command = _deleteChatCommand.Bind(userId2, chatId);
        user2Command.SetConsistencyLevel(_operationOptions.DeleteUserChat.ConsistencyLevel);
        user2Command.SetIdempotence(true);

        _dbContext.Session.Execute(user1Command);
        _dbContext.Session.Execute(user2Command);
        _logger.LogTrace("Deleted user chats between user {User1} and user {User2}", userId1, userId2);
    }
}
