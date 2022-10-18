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
    public List<ChatState> Chats { get; set; }
}

public sealed class ChatState
{
    [AliasAs("chatID")]
    public string ChatID { get; set; }

    [AliasAs("newestMessage")]
    public long NewestMessage { get; set; }

    [AliasAs("otherUserDelivered")]
    public long OtherUserDelivered { get; set; }

    [AliasAs("otherUserSeen")]
    public long OtherUserSeen { get; set; }
}