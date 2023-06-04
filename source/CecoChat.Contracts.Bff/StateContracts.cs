using Refit;

namespace CecoChat.Contracts.Bff;

public sealed class GetChatsRequest
{
    [AliasAs("newerThan")]
    public DateTime NewerThan { get; init; }
}

public sealed class GetChatsResponse
{
    [AliasAs("chats")]
    public ChatState[] Chats { get; init; } = Array.Empty<ChatState>();
}

public sealed class ChatState
{
    [AliasAs("chatID")]
    public string ChatId { get; init; } = string.Empty;

    [AliasAs("newestMessage")]
    public long NewestMessage { get; init; }

    [AliasAs("otherUserDelivered")]
    public long OtherUserDelivered { get; init; }

    [AliasAs("otherUserSeen")]
    public long OtherUserSeen { get; init; }
}
