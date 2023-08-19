using System.Text.Json.Serialization;
using Refit;

namespace CecoChat.Contracts.Bff.Chats;

public sealed class GetChatsRequest
{
    [JsonPropertyName("newerThan")]
    [AliasAs("newerThan")]
    public DateTime NewerThan { get; init; }
}

public sealed class GetChatsResponse
{
    [JsonPropertyName("chats")]
    [AliasAs("chats")]
    public ChatState[] Chats { get; init; } = Array.Empty<ChatState>();
}

public sealed class ChatState
{
    [JsonPropertyName("chatId")]
    [AliasAs("chatId")]
    public string ChatId { get; init; } = string.Empty;

    [JsonPropertyName("newestMessage")]
    [AliasAs("newestMessage")]
    public long NewestMessage { get; init; }

    [JsonPropertyName("otherUserDelivered")]
    [AliasAs("otherUserDelivered")]
    public long OtherUserDelivered { get; init; }

    [JsonPropertyName("otherUserSeen")]
    [AliasAs("otherUserSeen")]
    public long OtherUserSeen { get; init; }
}
