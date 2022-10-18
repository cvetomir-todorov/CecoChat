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
    public List<HistoryMessage> Messages { get; set; }
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
    public string Data { get; set; }

    [AliasAs("reactions")]
    public Dictionary<long, string> Reactions { get; set; }
}

public enum DataType
{
    [EnumMember(Value = "plainText")]
    PlainText
}