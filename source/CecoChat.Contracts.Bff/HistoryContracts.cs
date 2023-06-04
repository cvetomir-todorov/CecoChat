using System.Collections.Immutable;
using System.Runtime.Serialization;
using Refit;

namespace CecoChat.Contracts.Bff;

public sealed class GetHistoryRequest
{
    [AliasAs("otherUserId")]
    public long OtherUserId { get; init; }

    [AliasAs("olderThan")]
    public DateTime OlderThan { get; init; }
}

public sealed class GetHistoryResponse
{
    [AliasAs("messages")]
    public HistoryMessage[] Messages { get; init; } = Array.Empty<HistoryMessage>();
}

public sealed class HistoryMessage
{
    [AliasAs("messageId")]
    public long MessageId { get; init; }

    [AliasAs("senderId")]
    public long SenderId { get; init; }

    [AliasAs("receiverId")]
    public long ReceiverId { get; init; }

    [AliasAs("dataType")]
    public DataType DataType { get; set; }

    [AliasAs("data")]
    public string Data { get; set; } = string.Empty;

    [AliasAs("reactions")]
    public IDictionary<long, string> Reactions { get; set; } = ImmutableDictionary<long, string>.Empty;
}

public enum DataType
{
    [EnumMember(Value = "plainText")]
    PlainText
}
