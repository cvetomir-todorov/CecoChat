using CecoChat.Contracts.Messaging;
using CecoChat.Data.Config.Partitioning;
using CecoChat.Events;
using CecoChat.Kafka;
using CecoChat.Server.Backplane;
using CecoChat.Server.Messaging.Backplane;
using CecoChat.Server.Messaging.Clients;
using CecoChat.Server.Messaging.Endpoints;

namespace CecoChat.Server.Messaging.HostedServices;

public sealed class HandlePartitionsChanged : IHostedService, ISubscriber<PartitionsChangedEventData>
{
    private readonly ILogger _logger;
    private readonly IBackplaneComponents _backplaneComponents;
    private readonly IPartitionUtility _partitionUtility;
    private readonly IClientContainer _clientContainer;
    private readonly IEvent<PartitionsChangedEventData> _partitionsChanged;
    private readonly Guid _partitionsChangedToken;

    public HandlePartitionsChanged(
        ILogger<HandlePartitionsChanged> logger,
        IBackplaneComponents backplaneComponents,
        IPartitionUtility partitionUtility,
        IClientContainer clientContainer,
        IEvent<PartitionsChangedEventData> partitionsChanged)
    {
        _logger = logger;
        _backplaneComponents = backplaneComponents;
        _partitionUtility = partitionUtility;
        _clientContainer = clientContainer;
        _partitionsChanged = partitionsChanged;

        _partitionsChangedToken = _partitionsChanged.Subscribe(this);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _partitionsChanged.Unsubscribe(_partitionsChangedToken);
        return Task.CompletedTask;
    }

    public async ValueTask Handle(PartitionsChangedEventData eventData)
    {
        int partitionCount = eventData.PartitionCount;
        PartitionRange partitions = eventData.Partitions;

        await DisconnectClients(partitionCount, partitions);
        _backplaneComponents.ConfigurePartitioning(partitionCount, partitions);
    }

    private async ValueTask DisconnectClients(int partitionCount, PartitionRange partitions)
    {
        ListenNotification notification = new() { Type = MessageType.Disconnect };
        int userCount = 0;

        foreach (long userId in _clientContainer.EnumerateUsers())
        {
            int userPartition = _partitionUtility.ChoosePartition(userId, partitionCount);
            if (!partitions.Contains(userPartition))
            {
                await _clientContainer.NotifyInGroup(notification, userId);
                userCount++;
            }
        }

        _logger.LogInformation("Disconnected {UserCount} users", userCount);
    }
}
