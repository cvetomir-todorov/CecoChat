using CecoChat.DynamicConfig.Sections.Partitioning;
using CecoChat.Messaging.Contracts;
using CecoChat.Server.Backplane;
using CecoChat.Server.Messaging.Backplane;
using CecoChat.Server.Messaging.Clients;
using Common.AspNet.Init;
using Common.Events;
using Common.Kafka;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.Messaging.Init;

public sealed class BackplaneComponentsInit : InitStep, ISubscriber<PartitionsChangedEventArgs>
{
    private readonly ILogger _logger;
    private readonly ConfigOptions _configOptions;
    private readonly IBackplaneComponents _backplaneComponents;
    private readonly IPartitioningConfig _partitioningConfig;
    private readonly IPartitioner _partitioner;
    private readonly IClientContainer _clientContainer;
    private readonly IEvent<PartitionsChangedEventArgs> _partitionsChanged;
    private readonly Guid _partitionsChangedToken;

    public BackplaneComponentsInit(
        ILogger<BackplaneComponentsInit> logger,
        IHostApplicationLifetime applicationLifetime,
        IOptions<ConfigOptions> configOptions,
        IBackplaneComponents backplaneComponents,
        IPartitioningConfig partitioningConfig,
        IPartitioner partitioner,
        IClientContainer clientContainer,
        IEvent<PartitionsChangedEventArgs> partitionsChanged)
        : base(applicationLifetime)
    {
        _logger = logger;
        _configOptions = configOptions.Value;
        _backplaneComponents = backplaneComponents;
        _partitioningConfig = partitioningConfig;
        _partitioner = partitioner;
        _clientContainer = clientContainer;
        _partitionsChanged = partitionsChanged;

        _partitionsChangedToken = _partitionsChanged.Subscribe(this);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _partitionsChanged.Unsubscribe(_partitionsChangedToken);
            _backplaneComponents.Dispose();
        }
    }

    protected override Task<bool> DoExecute(CancellationToken ct)
    {
        int partitionCount = _partitioningConfig.PartitionCount;
        PartitionRange partitions = _partitioningConfig.GetPartitions(_configOptions.ServerId);

        _backplaneComponents.ConfigurePartitioning(partitionCount, partitions);
        _backplaneComponents.StartConsumption(ct);

        return Task.FromResult(true);
    }

    public async ValueTask Handle(PartitionsChangedEventArgs _)
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
