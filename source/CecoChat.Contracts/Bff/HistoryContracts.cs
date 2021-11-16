using System;
using System.Collections.Generic;

namespace CecoChat.Contracts.Bff
{
    public sealed class GetHistoryRequest
    {
        public long OtherUserID { get; set; }
        public DateTime OlderThan { get; set; }
    }

    public sealed class GetHistoryResponse
    {
        public List<HistoryMessage> Messages { get; set; }
    }

    public sealed class HistoryMessage
    {
        public long MessageID { get; set; }
        public long SenderID { get; set; }
        public long ReceiverID { get; set; }
        public DataType DataType { get; set; }
        public string Data { get; set; }
        public Dictionary<long, string> Reactions { get; set; }
    }

    public enum DataType
    {
        PlainText
    }
}