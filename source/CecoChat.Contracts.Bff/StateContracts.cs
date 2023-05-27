using Refit;

namespace CecoChat.Contracts.Bff;

public sealed class GetChatsRequest
{
    [AliasAs("newerThan")]
    public DateTime NewerThan { get; set; }
}

public sealed class GetChatsResponse
{
    [AliasAs("chats")]
    public ChatState[] Chats { get; set; } = Array.Empty<ChatState>();
}

public sealed class ChatState
{
    [AliasAs("chatID")]
    public string ChatID { get; set; } = string.Empty;

    [AliasAs("newestMessage")]
    public long NewestMessage { get; set; }

    [AliasAs("otherUserDelivered")]
    public long OtherUserDelivered { get; set; }

    [AliasAs("otherUserSeen")]
    public long OtherUserSeen { get; set; }
}
