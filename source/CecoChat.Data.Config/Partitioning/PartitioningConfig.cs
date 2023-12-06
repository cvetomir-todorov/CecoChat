using CecoChat.Events;
using CecoChat.Kafka;
using CecoChat.Redis;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CecoChat.Data.Config.Partitioning;

internal sealed class PartitioningConfig : IPartitioningConfig
{
    private readonly ILogger _logger;
    private readonly IRedisContext _redisContext;
    private readonly IPartitioningRepo _repo;
    private readonly IConfigUtility _configUtility;
    private readonly IEventSource<PartitionsChangedEventData> _partitionsChanged;

    private PartitioningConfigUsage? _usage;
    private PartitioningValidator? _validator;
    private PartitioningValues? _values;

    public PartitioningConfig(
        ILogger<PartitioningConfig> logger,
        IRedisContext redisContext,
        IPartitioningRepo repo,
        IConfigUtility configUtility,
        IEventSource<PartitionsChangedEventData> partitionsChanged)
    {
        _logger = logger;
        _redisContext = redisContext;
        _repo = repo;
        _configUtility = configUtility;
        _partitionsChanged = partitionsChanged;
    }

    public void Dispose()
    {
        _redisContext.Dispose();
    }

    public int PartitionCount
    {
        get
        {
            EnsureInitialized();
            return _values!.PartitionCount;
        }
    }

    public PartitionRange GetPartitions(string server)
    {
        EnsureInitialized();

        if (!_values!.ServerPartitionMap.TryGetValue(server, out PartitionRange partitions))
        {
            throw new InvalidOperationException($"No partitions configured for server {server}.");
        }

        return partitions;
    }

    public string GetAddress(int partition)
    {
        EnsureInitialized();

        if (!_values!.PartitionServerMap.TryGetValue(partition, out string? server))
        {
            throw new InvalidOperationException($"No server configured for partition {partition}.");
        }
        if (!_values.ServerAddressMap.TryGetValue(server, out string? address))
        {
            throw new InvalidOperationException($"No address configured for server {server}.");
        }

        return address;
    }

    public async Task Initialize(PartitioningConfigUsage usage)
    {
        try
        {
            _usage = usage;
            _validator = new PartitioningValidator(usage);
            await SubscribeForChanges(usage);
            await LoadValidateValues(usage);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Initializing partitioning configuration failed");
        }
    }

    private async Task SubscribeForChanges(PartitioningConfigUsage usage)
    {
        ISubscriber subscriber = _redisContext.GetSubscriber();

        if (usage.UsePartitions || usage.UseAddresses)
        {
            RedisChannel channel = new($"notify:{PartitioningKeys.Partitions}", RedisChannel.PatternMode.Literal);
            ChannelMessageQueue messageQueue = await subscriber.SubscribeAsync(channel);
            messageQueue.OnMessage(channelMessage => _configUtility.HandleChange(channelMessage, HandlePartitions));
            _logger.LogInformation("Subscribed for changes about {PartitionCount}, {ServerPartitions} from channel {Channel}",
                PartitioningKeys.PartitionCount, PartitioningKeys.Partitions, messageQueue.Channel);
        }
        if (usage.UseAddresses)
        {
            RedisChannel channel = new($"notify:{PartitioningKeys.Addresses}", RedisChannel.PatternMode.Literal);
            ChannelMessageQueue messageQueue = await subscriber.SubscribeAsync(channel);
            messageQueue.OnMessage(channelMessage => _configUtility.HandleChange(channelMessage, HandleAddresses));
            _logger.LogInformation("Subscribed for changes about {ServerAddresses} from channel {Channel}",
                PartitioningKeys.Addresses, messageQueue.Channel);
        }
    }

    private async Task HandlePartitions(ChannelMessage _)
    {
        EnsureInitialized();

        if (!_usage!.UsePartitions && !_usage.UseAddresses)
        {
            return;
        }

        bool areValid = await LoadValidateValues(_usage);
        if (!areValid)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(_usage.ServerToWatch))
        {
            PartitioningValues values = _values!;
            if (values.ServerPartitionMap.TryGetValue(_usage.ServerToWatch, out PartitionRange partitions))
            {
                PartitionsChangedEventData eventData = new()
                {
                    PartitionCount = values.PartitionCount,
                    Partitions = partitions
                };

                _partitionsChanged.Publish(eventData);
            }
            else
            {
                _logger.LogError("After server partition changes there are no partitions for watched server {Server}", _usage.ServerToWatch);
            }
        }
        else
        {
            PartitioningValues values = _values!;
            PartitionsChangedEventData eventData = new()
            {
                PartitionCount = values.PartitionCount,
                Partitions = PartitionRange.Empty
            };

            _partitionsChanged.Publish(eventData);
        }
    }

    private async Task HandleAddresses(ChannelMessage _)
    {
        EnsureInitialized();

        if (_usage!.UseAddresses)
        {
            await LoadValidateValues(_usage);
        }
    }

    private async Task<bool> LoadValidateValues(PartitioningConfigUsage usage)
    {
        EnsureInitialized();

        PartitioningValues values = await _repo.GetValues(usage);
        _logger.LogInformation("Loading partitioning configuration succeeded");

        bool areValid = _configUtility.ValidateValues("partitioning", values, _validator!);
        if (areValid)
        {
            _values = values;
            PrintValues(usage, values);
        }

        return areValid;
    }

    private void PrintValues(PartitioningConfigUsage usage, PartitioningValues values)
    {
        _logger.LogInformation("Partition count set to {PartitionCount}", values.PartitionCount);

        if (usage.UsePartitions || usage.UseAddresses)
        {
            foreach (KeyValuePair<string, PartitionRange> pair in values.ServerPartitionMap)
            {
                string server = pair.Key;
                PartitionRange partitions = pair.Value;
                _logger.LogInformation("Partitions {Partitions} are assigned to server {Server}", partitions, server);
            }
        }
        if (usage.UseAddresses)
        {
            foreach (KeyValuePair<string, string> pair in values.ServerAddressMap)
            {
                string server = pair.Key;
                string address = pair.Value;
                _logger.LogInformation("Address {Address} is assigned to server {Server}", address, server);
            }
        }
    }

    private void EnsureInitialized()
    {
        if (_usage == null || _validator == null)
        {
            throw new InvalidOperationException($"Call '{nameof(Initialize)}' to initialize the config.");
        }
    }
}
