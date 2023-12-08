using System.Diagnostics.CodeAnalysis;
using CecoChat.Data.Config.Common;
using CecoChat.Events;
using CecoChat.Kafka;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.Config.Partitioning;

public interface IPartitioningConfig
{
    Task<bool> Initialize();

    int PartitionCount { get; }

    PartitionRange GetPartitions(string server);

    string GetAddress(int partition);
}

internal sealed class PartitioningConfig : IPartitioningConfig
{
    private readonly ILogger _logger;
    private readonly IConfigSection<PartitioningValues> _section;
    // TODO: raise the event
    private readonly IEventSource<EventArgs> _partitionsChanged;
    private PartitioningValues? _values;

    public PartitioningConfig(
        ILogger<PartitioningConfig> logger,
        IConfigSection<PartitioningValues> section,
        IEventSource<EventArgs> partitionsChanged)
    {
        _logger = logger;
        _section = section;
        _partitionsChanged = partitionsChanged;
    }

    public int PartitionCount
    {
        get
        {
            EnsureInitialized();
            return _values.PartitionCount;
        }
    }

    public PartitionRange GetPartitions(string server)
    {
        EnsureInitialized();

        if (!_values.ServerPartitionMap.TryGetValue(server, out PartitionRange partitions))
        {
            throw new InvalidOperationException($"No partitions configured for server {server}.");
        }

        return partitions;
    }

    public string GetAddress(int partition)
    {
        EnsureInitialized();

        if (!_values.PartitionServerMap.TryGetValue(partition, out string? server))
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
        InitializeResult<PartitioningValues> result = await _section.Initialize(ConfigKeys.Partitioning.Section, PrintValues);
        _values = result.Values;

        return result.Success;
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

    [MemberNotNull(nameof(_values))]
    private void EnsureInitialized()
    {
        if (_values == null)
        {
            throw new InvalidOperationException($"Call '{nameof(Initialize)}' to initialize the config.");
        }
    }
}
