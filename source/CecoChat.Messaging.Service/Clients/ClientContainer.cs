using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using CecoChat.Messaging.Contracts;
using CecoChat.Messaging.Service.Endpoints;
using CecoChat.Messaging.Service.Telemetry;
using Common.AspNet.SignalR.Telemetry;
using Microsoft.AspNetCore.SignalR;

namespace CecoChat.Messaging.Service.Clients;

public interface IClientContainer
{
    ValueTask AddClient(long userId, string connectionId);

    ValueTask RemoveClient(long userId, string connectionId);

    Task NotifyInGroup(ListenNotification notification, long groupUserId, string? excluding = null);

    IEnumerable<long> EnumerateUsers();
}

public class ClientContainer : IClientContainer
{
    private readonly ILogger _logger;
    private readonly IHubContext<ChatHub, IChatListener> _hubContext;
    private readonly IMessagingTelemetry _messagingTelemetry;
    private readonly ISignalRTelemetry _signalRTelemetry;
    private readonly ConcurrentDictionary<long, ClientContext> _clients;

    public ClientContainer(
        ILogger<ClientContainer> logger,
        IHubContext<ChatHub, IChatListener> hubContext,
        IMessagingTelemetry messagingTelemetry,
        ISignalRTelemetry signalRTelemetry)
    {
        _logger = logger;
        _hubContext = hubContext;
        _messagingTelemetry = messagingTelemetry;
        _signalRTelemetry = signalRTelemetry;

        // max 64 000 clients per server
        _clients = new(capacity: 8000, concurrencyLevel: Environment.ProcessorCount);
    }

    public async ValueTask AddClient(long userId, string connectionId)
    {
        _messagingTelemetry.AddOnlineClient();
        await _hubContext.Groups.AddToGroupAsync(connectionId, GetGroupName(userId));
        ClientContext current = _clients.GetOrAdd(userId, new ClientContext());
        current.IncreaseCount();
    }

    public async ValueTask RemoveClient(long userId, string connectionId)
    {
        _messagingTelemetry.RemoveOnlineClient();
        await _hubContext.Groups.RemoveFromGroupAsync(connectionId, GetGroupName(userId));

        if (_clients.TryGetValue(userId, out ClientContext? current))
        {
            current.DecreaseCount();
        }
        else
        {
            throw new InvalidOperationException("When decreasing count of clients for a specific user, it should had been already added.");
        }
    }

    public async Task NotifyInGroup(ListenNotification notification, long groupUserId, string? excluding = null)
    {
        string group = GetGroupName(groupUserId);
        Activity? activity = _signalRTelemetry.StartGroupSendActivity(nameof(ChatHub), group, activity =>
        {
            activity.SetTag("cecochat.messaging.notification_type", notification.Type);
        });

        try
        {
            await DoNotifyInGroup(notification, group, excluding);
            _signalRTelemetry.Succeed(activity);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to send notification {NotificationType} to clients in group {ClientGroup}", notification.Type, group);
            _signalRTelemetry.Fail(activity, exception);
        }
    }

    private Task DoNotifyInGroup(ListenNotification notification, string group, string? excluding)
    {
        Task result;
        if (excluding == null)
        {
            result = _hubContext.Clients.Group(group).Notify(notification);
        }
        else
        {
            result = _hubContext.Clients.GroupExcept(group, excludedConnectionId1: excluding).Notify(notification);
        }

        _logger.LogTrace("Sent notification for message {MessageId} from sender {SenderId} to receiver {Receiver} of type {NotificationType} to group {Group} (excluding {Excluding})",
            notification.MessageId, notification.SenderId, notification.ReceiverId, notification.Type, group, excluding);
        return result;
    }

    public IEnumerable<long> EnumerateUsers()
    {
        foreach (KeyValuePair<long, ClientContext> pair in _clients)
        {
            long userId = pair.Key;
            ClientContext context = pair.Value;

            if (context.Count > 0)
            {
                yield return userId;
            }
        }
    }

    private static string GetGroupName(long userId)
    {
        return userId.ToString(CultureInfo.InvariantCulture);
    }

    private sealed class ClientContext
    {
        private int _count;

        public int Count => _count;

        public void IncreaseCount()
        {
            Interlocked.Increment(ref _count);
        }

        public void DecreaseCount()
        {
            Interlocked.Decrement(ref _count);
        }
    }
}
