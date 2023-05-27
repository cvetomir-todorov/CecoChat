using System.Collections.Immutable;
using System.Runtime.Serialization;
using Refit;

namespace CecoChat.Contracts.Bff;

public sealed class GetHistoryRequest
{
    [AliasAs("otherUserID")]
    public long OtherUserID { get; set; }

    [AliasAs("olderThan")]
    public DateTime OlderThan { get; set; }
}

public sealed class GetHistoryResponse
{
    [AliasAs("messages")]
    public HistoryMessage[] Messages { get; set; } = Array.Empty<HistoryMessage>();
}

public sealed class HistoryMessage
{
    [AliasAs("messageID")]
    public long MessageID { get; set; }

    [AliasAs("senderID")]
    public long SenderID { get; set; }

    [AliasAs("receiverID")]
    public long ReceiverID { get; set; }

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
