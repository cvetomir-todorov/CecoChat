using CecoChat.Contracts.Messaging;

namespace CecoChat.Client.Messaging;

public interface IMessagingClient : IAsyncDisposable
{
    Task Connect(string messagingServerAddress, string accessToken, CancellationToken ct);

    Task<SendMessageResponse> SendMessage(SendMessageRequest request);

    Task<ReactResponse> React(ReactRequest request);

    Task<UnReactResponse> UnReact(UnReactRequest request);

    event EventHandler<ListenNotification>? MessageReceived;

    event EventHandler<ListenNotification>? ReactionReceived;

    event EventHandler<ListenNotification>? MessageDelivered;

    event EventHandler? Disconnected;
}
