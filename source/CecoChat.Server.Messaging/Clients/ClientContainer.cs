using System.Collections.Concurrent;
using System.Globalization;
using CecoChat.Contracts.Messaging;
using CecoChat.Server.Messaging.Telemetry;
using Microsoft.AspNetCore.SignalR;

namespace CecoChat.Server.Messaging.Clients;

public interface IClientContainer
{
    ValueTask AddClient(long userId, string connectionId);

    ValueTask RemoveClient(long userId, string connectionId);

    ValueTask NotifyInGroup(ListenNotification notification, long groupUserId, string? excluding = null);

    IEnumerable<long> EnumerateUsers();
}

public class ClientContainer : IClientContainer
{
    private readonly IHubContext<ChatHub, IChatListener> _hubContext;
    private readonly IMessagingTelemetry _messagingTelemetry;
    private readonly ConcurrentDictionary<long, ClientContext> _clients;

    public ClientContainer(
        IMessagingTelemetry messagingTelemetry,
        IHubContext<ChatHub, IChatListener> hubContext)
    {
        _messagingTelemetry = messagingTelemetry;
        _hubContext = hubContext;
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

    public async ValueTask NotifyInGroup(ListenNotification notification, long groupUserId, string? excluding = null)
    {
        string group = GetGroupName(groupUserId);
        if (excluding == null)
        {
            await _hubContext.Clients.Group(group).Notify(notification);
        }
        else
        {
            await _hubContext.Clients.GroupExcept(group, excludedConnectionId1: excluding).Notify(notification);
        }
    }

    public IEnumerable<long> EnumerateUsers()
    {
        foreach (KeyValuePair<long,ClientContext> pair in _clients)
        {
            long userId = pair.Key;
            ClientContext context = pair.Value;

            if (context.Count > 0)
            {
                yield return userId;
            }
        }
    }

    private string GetGroupName(long userId)
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
