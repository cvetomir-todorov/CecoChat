namespace CecoChat.Contracts.History
{
    public sealed class HistoryMessage
    {
        public long MessageID { get; set; }
        public long SenderID { get; set; }
        public long ReceiverID { get; set; }
        public HistoryMessageType Type { get; set; }
        public string Text { get; set; }
    }

    public enum HistoryMessageType
    {
        Unknown,
        PlainText,
        Ack
    }
}