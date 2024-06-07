using Cassandra;

namespace CecoChat.Chats.Data;

public sealed class ChatMessagesOperationOptions
{
    public OperationOptions GetHistory { get; init; } = null!;
    public OperationOptions AddPlainTextMessage { get; init; } = null!;
    public OperationOptions AddFileMessage { get; init; } = null!;
    public OperationOptions SetReaction { get; init; } = null!;
    public OperationOptions UnsetReaction { get; init; } = null!;
    public OperationOptions DeleteChat { get; init; } = null!;
}

public sealed class UserChatsOperationOptions
{
    public OperationOptions GetUserChats { get; init; } = null!;
    public OperationOptions GetUserChat { get; init; } = null!;
    public OperationOptions UpdateUserChat { get; init; } = null!;
}

public sealed class OperationOptions
{
    public ConsistencyLevel ConsistencyLevel { get; init; }
}
