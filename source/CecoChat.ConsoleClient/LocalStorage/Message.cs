namespace CecoChat.ConsoleClient.LocalStorage;

public sealed class Message
{
    public long MessageId { get; init; }
    public long SenderId { get; init; }
    public long ReceiverId { get; init; }
    public string Text { get; set; } = string.Empty;
    public MessageType Type { get; set; }
    public string FileBucket { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public IDictionary<long, string> Reactions { get; }

    public Message()
    {
        Reactions = new Dictionary<long, string>(capacity: 2);
    }
}

public enum MessageType
{
    PlainText, File
}
