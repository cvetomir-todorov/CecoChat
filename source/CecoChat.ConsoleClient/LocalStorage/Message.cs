namespace CecoChat.ConsoleClient.LocalStorage;

public sealed class Message
{
    public long MessageId { get; init; }
    public long SenderId { get; init; }
    public long ReceiverId { get; init; }
    public string Text { get; set; } = string.Empty;
    public IDictionary<long, string> Reactions { get; }

    public Message()
    {
        Reactions = new Dictionary<long, string>(capacity: 2);
    }
}
