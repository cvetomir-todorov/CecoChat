namespace CecoChat.Contracts.Messaging;

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
    public NotificationData? Data { get; set; }
    public NotificationReaction? Reaction { get; set; }
    public NotificationConnection? Connection { get; set; }
}

public sealed class NotificationData
{
    public DataType Type { get; init; }
    public string Data { get; init; } = string.Empty;
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
