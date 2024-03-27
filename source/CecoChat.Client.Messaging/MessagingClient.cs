using CecoChat.Messaging.Contracts;
using Common;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Client.Messaging;

public sealed class MessagingClient : IMessagingClient
{
    private readonly HubConnection _client;

    public MessagingClient(string accessToken, string messagingServerAddress)
    {
        _client = CreateClient(accessToken, messagingServerAddress);
    }

    public ValueTask DisposeAsync()
    {
        return _client.DisposeAsync();
    }

    private static HubConnection CreateClient(string accessToken, string messagingServerAddress)
    {
        UriBuilder uriBuilder = new(messagingServerAddress);
        if (uriBuilder.Path.EndsWith('/'))
        {
            uriBuilder.Path += "chat";
        }
        else
        {
            uriBuilder.Path += "/chat";
        }

        HubConnection client = new HubConnectionBuilder()
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
        client.ServerTimeout = TimeSpan.FromDays(1);

        // interval during which client sends ping to the server
        // we don't want the test console client to keep server resources
        client.KeepAliveInterval = TimeSpan.FromDays(1);

        return client;
    }

    public async Task Connect(CancellationToken ct)
    {
        _client.On<ListenNotification>(nameof(IChatListener.Notify), notification =>
        {
            switch (notification.Type)
            {
                case MessageType.PlainText:
                    PlainTextReceived?.Invoke(this, notification);
                    break;
                case MessageType.File:
                    FileReceived?.Invoke(this, notification);
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
                case MessageType.Connection:
                    ConnectionNotificationReceived?.Invoke(this, notification);
                    break;
                default:
                    throw new EnumValueNotSupportedException(notification.Type);
            }
        });

        await _client.StartAsync(ct);
    }

    public async Task<long> SendPlainTextMessage(long receiverId, string text)
    {
        SendPlainTextRequest request = new()
        {
            ReceiverId = receiverId,
            Text = text
        };
        SendPlainTextResponse response = await _client.InvokeAsync<SendPlainTextResponse>(nameof(IChatHub.SendPlainText), request);

        return response.MessageId;
    }

    public async Task<long> SendFileMessage(long receiverId, string text, string fileBucket, string filePath)
    {
        SendFileRequest request = new()
        {
            ReceiverId = receiverId,
            Text = text,
            Bucket = fileBucket,
            Path = filePath
        };
        SendFileResponse response = await _client.InvokeAsync<SendFileResponse>(nameof(IChatHub.SendFile), request);

        return response.MessageId;
    }

    public Task React(long messageId, long senderId, long receiverId, string reaction)
    {
        ReactRequest request = new()
        {
            MessageId = messageId,
            SenderId = senderId,
            ReceiverId = receiverId,
            Reaction = reaction
        };
        return _client.InvokeAsync<ReactResponse>(nameof(IChatHub.React), request);
    }

    public Task UnReact(long messageId, long senderId, long receiverId)
    {
        UnReactRequest request = new()
        {
            MessageId = messageId,
            SenderId = senderId,
            ReceiverId = receiverId
        };
        return _client.InvokeAsync<UnReactResponse>(nameof(IChatHub.UnReact), request);
    }

    public event EventHandler<ListenNotification>? PlainTextReceived;

    public event EventHandler<ListenNotification>? FileReceived;

    public event EventHandler<ListenNotification>? ReactionReceived;

    public event EventHandler<ListenNotification>? MessageDelivered;

    public event EventHandler<ListenNotification>? ConnectionNotificationReceived;

    public event EventHandler? Disconnected;
}
