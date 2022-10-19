namespace CecoChat.ConsoleClient.LocalStorage;

public sealed class Message
{
    public long MessageID { get; set; }
    public long SenderID { get; set; }
    public long ReceiverID { get; set; }
    public DataType DataType { get; set; }
    public string Data { get; set; } = string.Empty;
    public IDictionary<long, string> Reactions { get; }
    public int SequenceNumber { get; set; }

    public Message()
    {
        Reactions = new Dictionary<long, string>(capacity: 2);
    }
}

public enum DataType
{
    PlainText
}