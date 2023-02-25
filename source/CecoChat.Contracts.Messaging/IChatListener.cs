namespace CecoChat.Contracts.Messaging;

public interface IChatListener
{
    Task Notify(ListenNotification notification);
}

public sealed class ListenNotification
{
    public long MessageId { get; set; }
    public long SenderId { get; set; }
    public long ReceiverId { get; set; }
    public MessageType Type { get; set; }
    public DeliveryStatus DeliveryStatus { get; set; }
    public NotificationData? Data { get; set; }
    public NotificationReaction? Reaction { get; set; }
}

public sealed class NotificationData
{
    public DataType Type { get; set; }
    public string Data { get; set; } = string.Empty;
}

public sealed class NotificationReaction
{
    public long ReactorId { get; set; }
    public string Reaction { get; set; } = string.Empty;
}
