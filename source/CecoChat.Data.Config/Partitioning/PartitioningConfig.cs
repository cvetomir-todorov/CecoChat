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
            _validator = new PartitioningValidator();
            await SubscribeForChanges();
            await LoadValidateValues();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Initializing partitioning configuration failed");
        }
    }

    private async Task SubscribeForChanges()
    {
        ISubscriber subscriber = _redisContext.GetSubscriber();

        RedisChannel partitionsChannel = new($"notify:{PartitioningKeys.Partitions}", RedisChannel.PatternMode.Literal);
        ChannelMessageQueue partitionsMessageQueue = await subscriber.SubscribeAsync(partitionsChannel);
        partitionsMessageQueue.OnMessage(channelMessage => _configUtility.HandleChange(channelMessage, HandlePartitions));
        _logger.LogInformation("Subscribed for changes about {PartitionCount}, {ServerPartitions} from channel {Channel}",
            PartitioningKeys.PartitionCount, PartitioningKeys.Partitions, partitionsMessageQueue.Channel);

        RedisChannel addressesChannel = new($"notify:{PartitioningKeys.Addresses}", RedisChannel.PatternMode.Literal);
        ChannelMessageQueue addressesMessageQueue = await subscriber.SubscribeAsync(addressesChannel);
        addressesMessageQueue.OnMessage(channelMessage => _configUtility.HandleChange(channelMessage, HandleAddresses));
        _logger.LogInformation("Subscribed for changes about {ServerAddresses} from channel {Channel}",
            PartitioningKeys.Addresses, addressesMessageQueue.Channel);
    }

    private async Task HandlePartitions(ChannelMessage _)
    {
        EnsureInitialized();

        bool areValid = await LoadValidateValues();
        if (!areValid)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(_usage!.ServerToWatch))
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
        await LoadValidateValues();
    }

    private async Task<bool> LoadValidateValues()
    {
        EnsureInitialized();

        PartitioningValues values = await _repo.GetValues();
        _logger.LogInformation("Loading partitioning configuration succeeded");

        bool areValid = _configUtility.ValidateValues("partitioning", values, _validator!);
        if (areValid)
        {
            _values = values;
            PrintValues(values);
        }

        return areValid;
    }

    private void PrintValues(PartitioningValues values)
    {
        _logger.LogInformation("Partition count set to {PartitionCount}", values.PartitionCount);

        foreach (KeyValuePair<string, PartitionRange> pair in values.ServerPartitionMap)
        {
            string server = pair.Key;
            PartitionRange partitions = pair.Value;
            _logger.LogInformation("Partitions {Partitions} are assigned to server {Server}", partitions, server);
        }

        foreach (KeyValuePair<string, string> pair in values.ServerAddressMap)
        {
            string server = pair.Key;
            string address = pair.Value;
            _logger.LogInformation("Address {Address} is assigned to server {Server}", address, server);
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
