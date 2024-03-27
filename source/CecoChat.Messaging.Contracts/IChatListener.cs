namespace CecoChat.Messaging.Contracts;

public interface IChatListener
{
    Task Notify(ListenNotification notification);
}

public sealed class ListenNotification
{
    public long MessageId { get; init; }
    public long SenderId { get; init; }
    public long ReceiverId { get; init; }
    public MessageType Type { get; set; }
    public DeliveryStatus DeliveryStatus { get; init; }
    public NotificationPlainText? PlainText { get; set; }
    public NotificationFile? File { get; set; }
    public NotificationReaction? Reaction { get; set; }
    public NotificationConnection? Connection { get; set; }
}

public sealed class NotificationPlainText
{
    public string Text { get; init; } = string.Empty;
}

public sealed class NotificationFile
{
    public string Text { get; init; } = string.Empty;
    public string Bucket { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
}

public sealed class NotificationReaction
{
    public long ReactorId { get; init; }
    public string Reaction { get; init; } = string.Empty;
}

public sealed class NotificationConnection
{
    public ConnectionStatus Status { get; init; }
    public DateTime Version { get; init; }
}
