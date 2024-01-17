using CecoChat.Contracts.Messaging;

namespace CecoChat.Client.Messaging;

public interface IMessagingClient : IAsyncDisposable
{
    Task Connect(CancellationToken ct);

    Task<long> SendPlainTextMessage(long receiverId, string text);

    Task React(long messageId, long senderId, long receiverId, string reaction);

    Task UnReact(long messageId, long senderId, long receiverId);

    event EventHandler<ListenNotification>? MessageReceived;

    event EventHandler<ListenNotification>? ReactionReceived;

    event EventHandler<ListenNotification>? MessageDelivered;

    event EventHandler<ListenNotification>? ConnectionNotificationReceived;

    event EventHandler? Disconnected;
}
