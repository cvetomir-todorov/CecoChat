using CecoChat.Messaging.Contracts;

namespace CecoChat.Messaging.Client;

public interface IMessagingClient : IAsyncDisposable
{
    Task Connect(CancellationToken ct);

    Task<long> SendPlainTextMessage(long receiverId, string text);

    Task<long> SendFileMessage(long receiverId, string text, string fileBucket, string filePath);

    Task React(long messageId, long senderId, long receiverId, string reaction);

    Task UnReact(long messageId, long senderId, long receiverId);

    event EventHandler<ListenNotification>? PlainTextReceived;

    event EventHandler<ListenNotification>? FileReceived;

    event EventHandler<ListenNotification>? ReactionReceived;

    event EventHandler<ListenNotification>? MessageDelivered;

    event EventHandler<ListenNotification>? ConnectionNotificationReceived;

    event EventHandler? Disconnected;
}
