using CecoChat.Contracts.Messaging;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.ConsoleClient.Api;

public sealed class MessagingClient : IAsyncDisposable
{
    private HubConnection? _messagingClient;

    private const string NotConnectedExMsg = "Client should be connected first.";

    public ValueTask DisposeAsync()
    {
        if (_messagingClient != null)
        {
            return _messagingClient.DisposeAsync();
        }

        return ValueTask.CompletedTask;
    }

    public async Task Connect(string messagingServerAddress, string accessToken, CancellationToken ct)
    {
        UriBuilder uriBuilder = new(messagingServerAddress);
        uriBuilder.Path = "/chat";

        _messagingClient = new HubConnectionBuilder()
            .WithUrl(uriBuilder.Uri, http =>
            {
                http.AccessTokenProvider = () => Task.FromResult(accessToken)!;
            })
            .WithAutomaticReconnect(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5) })
            .AddMessagePackProtocol()
            .Build();

        // client times out when there is nothing from the server within the interval
        // potentially reconnects automatically if enabled
        // we don't want the test console client to time out 
        _messagingClient.ServerTimeout = TimeSpan.FromDays(1);

        // interval during which client sends ping to the server
        // we don't want the test console client to keep server resources
        _messagingClient.KeepAliveInterval = TimeSpan.FromDays(1);

        _messagingClient.On<ListenNotification>(nameof(IChatListener.Notify), notification =>
        {
            switch (notification.Type)
            {
                case MessageType.Data:
                    MessageReceived?.Invoke(this, notification);
                    break;
                case MessageType.Disconnect:
                    Disconnected?.Invoke(this, EventArgs.Empty);
                    break;
                case MessageType.DeliveryStatus:
                    MessageDelivered?.Invoke(this, notification);
                    break;
                case MessageType.Reaction:
                    ReactionReceived?.Invoke(this, notification);
                    break;
                default:
                    throw new EnumValueNotSupportedException(notification.Type);
            }
        });

        await _messagingClient.StartAsync(ct);
    }

    public event EventHandler<ListenNotification>? MessageReceived;

    public event EventHandler<ListenNotification>? ReactionReceived;

    public event EventHandler<ListenNotification>? MessageDelivered;

    public event EventHandler? Disconnected;

    public Task<SendMessageResponse> SendMessage(SendMessageRequest request)
    {
        if (_messagingClient == null)
        {
            throw new InvalidOperationException(NotConnectedExMsg);
        }

        return _messagingClient.InvokeAsync<SendMessageResponse>(nameof(IChatHub.SendMessage), request);
    }

    public Task<ReactResponse> React(ReactRequest request)
    {
        if (_messagingClient == null)
        {
            throw new InvalidOperationException(NotConnectedExMsg);
        }

        return _messagingClient.InvokeAsync<ReactResponse>(nameof(IChatHub.React), request);
    }

    public Task<UnReactResponse> UnReact(UnReactRequest request)
    {
        if (_messagingClient == null)
        {
            throw new InvalidOperationException(NotConnectedExMsg);
        }

        return _messagingClient.InvokeAsync<UnReactResponse>(nameof(IChatHub.UnReact), request);
    }
}
