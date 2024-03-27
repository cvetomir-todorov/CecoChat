using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Refit;

namespace CecoChat.Bff.Contracts.Chats;

public sealed class GetChatHistoryRequest
{
    [JsonPropertyName("otherUserId")]
    [AliasAs("otherUserId")]
    public long OtherUserId { get; init; }

    [JsonPropertyName("olderThan")]
    [AliasAs("olderThan")]
    public DateTime OlderThan { get; init; }
}

public sealed class GetChatHistoryResponse
{
    [JsonPropertyName("messages")]
    [AliasAs("messages")]
    public HistoryMessage[] Messages { get; init; } = Array.Empty<HistoryMessage>();
}

public sealed class HistoryMessage
{
    [JsonPropertyName("messageId")]
    [AliasAs("messageId")]
    public long MessageId { get; init; }

    [JsonPropertyName("senderId")]
    [AliasAs("senderId")]
    public long SenderId { get; init; }

    [JsonPropertyName("receiverId")]
    [AliasAs("receiverId")]
    public long ReceiverId { get; init; }

    [JsonPropertyName("type")]
    [AliasAs("type")]
    public MessageType Type { get; set; }

    [JsonPropertyName("text")]
    [AliasAs("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("file")]
    [AliasAs("file")]
    public FileData? File { get; set; }

    [JsonPropertyName("reactions")]
    [AliasAs("reactions")]
    public IDictionary<long, string> Reactions { get; set; } = ImmutableDictionary<long, string>.Empty;
}

public enum MessageType
{
    [EnumMember(Value = "plainText")]
    PlainText,
    [EnumMember(Value = "file")]
    File
}

public sealed class FileData
{
    [JsonPropertyName("bucket")]
    [AliasAs("bucket")]
    public string Bucket { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    [AliasAs("path")]
    public string Path { get; init; } = string.Empty;
}
