using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Refit;

namespace CecoChat.Contracts.Bff.Chats;

public sealed class GetHistoryRequest
{
    [JsonPropertyName("otherUserId")]
    [AliasAs("otherUserId")]
    public long OtherUserId { get; init; }

    [JsonPropertyName("olderThan")]
    [AliasAs("olderThan")]
    public DateTime OlderThan { get; init; }
}

public sealed class GetHistoryResponse
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

    [JsonPropertyName("dataType")]
    [AliasAs("dataType")]
    public DataType DataType { get; set; }

    [JsonPropertyName("data")]
    [AliasAs("data")]
    public string Data { get; set; } = string.Empty;

    [JsonPropertyName("reactions")]
    [AliasAs("reactions")]
    public IDictionary<long, string> Reactions { get; set; } = ImmutableDictionary<long, string>.Empty;
}

public enum DataType
{
    [EnumMember(Value = "plainText")]
    PlainText
}
