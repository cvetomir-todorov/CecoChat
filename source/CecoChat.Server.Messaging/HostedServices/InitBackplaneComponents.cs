using CecoChat.Contracts.Messaging;
using CecoChat.DynamicConfig.Partitioning;
using CecoChat.Events;
using CecoChat.Kafka;
using CecoChat.Server.Backplane;
using CecoChat.Server.Messaging.Backplane;
using CecoChat.Server.Messaging.Clients;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.Messaging.HostedServices;

public sealed class InitBackplaneComponents : IHostedService, ISubscriber<EventArgs>, IDisposable
{
    private readonly ILogger _logger;
    private readonly ConfigOptions _configOptions;
    private readonly IBackplaneComponents _backplaneComponents;
    private readonly IPartitioningConfig _partitioningConfig;
    private readonly IPartitioner _partitioner;
    private readonly IClientContainer _clientContainer;
    private readonly IEvent<EventArgs> _partitionsChanged;
    private readonly Guid _partitionsChangedToken;
    private readonly CancellationToken _appStoppingCt;
    private CancellationTokenSource? _stoppedCts;

    public InitBackplaneComponents(
        ILogger<InitBackplaneComponents> logger,
        IHostApplicationLifetime applicationLifetime,
        IOptions<ConfigOptions> configOptions,
        IBackplaneComponents backplaneComponents,
        IPartitioningConfig partitioningConfig,
        IPartitioner partitioner,
        IClientContainer clientContainer,
        IEvent<EventArgs> partitionsChanged)
    {
        _logger = logger;
        _configOptions = configOptions.Value;
        _backplaneComponents = backplaneComponents;
        _partitioningConfig = partitioningConfig;
        _partitioner = partitioner;
        _clientContainer = clientContainer;
        _partitionsChanged = partitionsChanged;

        _appStoppingCt = applicationLifetime.ApplicationStopping;
        _partitionsChangedToken = _partitionsChanged.Subscribe(this);
    }

    public void Dispose()
    {
        _stoppedCts?.Dispose();
        _partitionsChanged.Unsubscribe(_partitionsChangedToken);
        _backplaneComponents.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _appStoppingCt);

        int partitionCount = _partitioningConfig.PartitionCount;
        PartitionRange partitions = _partitioningConfig.GetPartitions(_configOptions.ServerId);

        _backplaneComponents.ConfigurePartitioning(partitionCount, partitions);
        _backplaneComponents.StartConsumption(_stoppedCts.Token);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async ValueTask Handle(EventArgs _)
    {
        await DisconnectClients(
            previousPartitionCount: _backplaneComponents.CurrentPartitionCount,
            previousPartitions: _backplaneComponents.CurrentPartitions);

        int newPartitionCount = _partitioningConfig.PartitionCount;
        PartitionRange newPartitions = _partitioningConfig.GetPartitions(_configOptions.ServerId);

        _backplaneComponents.ConfigurePartitioning(newPartitionCount, newPartitions);
    }

    private async ValueTask DisconnectClients(int previousPartitionCount, PartitionRange previousPartitions)
    {
        ListenNotification notification = new()
        {
            Type = MessageType.Disconnect
        };
        int userCount = 0;

        foreach (long userId in _clientContainer.EnumerateUsers())
        {
            int userPartition = _partitioner.ChoosePartition(userId, previousPartitionCount);
            if (!previousPartitions.Contains(userPartition))
            {
                await _clientContainer.NotifyInGroup(notification, userId);
                userCount++;
            }
        }

        _logger.LogInformation("Disconnected {UserCount} users", userCount);
    }
}
