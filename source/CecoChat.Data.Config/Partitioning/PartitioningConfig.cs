using CecoChat.Events;
using CecoChat.Kafka;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.Config.Partitioning;

internal sealed class PartitioningConfig : IPartitioningConfig
{
    private readonly ILogger _logger;
    private readonly IPartitioningRepo _repo;
    private readonly IConfigUtility _configUtility;
    // TODO: raise the event
    private readonly IEventSource<EventArgs> _partitionsChanged;

    private PartitioningValidator? _validator;
    private PartitioningValues? _values;

    public PartitioningConfig(
        ILogger<PartitioningConfig> logger,
        IPartitioningRepo repo,
        IConfigUtility configUtility,
        IEventSource<EventArgs> partitionsChanged)
    {
        _logger = logger;
        _repo = repo;
        _configUtility = configUtility;
        _partitionsChanged = partitionsChanged;
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

    public async Task<bool> Initialize()
    {
        try
        {
            _validator = new PartitioningValidator();
            bool areValid = await LoadValidateValues();

            return areValid;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Initializing partitioning configuration failed");
            return false;
        }
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
        if (_validator == null)
        {
            throw new InvalidOperationException($"Call '{nameof(Initialize)}' to initialize the config.");
        }
    }
}
