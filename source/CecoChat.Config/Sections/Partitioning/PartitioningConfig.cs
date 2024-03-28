using Common.Events;
using Common.Kafka;
using Microsoft.Extensions.Logging;

namespace CecoChat.Config.Sections.Partitioning;

public interface IPartitioningConfig : IDisposable
{
    Task<bool> Initialize(CancellationToken ct);

    int PartitionCount { get; }

    PartitionRange GetPartitions(string server);

    string GetAddress(int partition);
}

public sealed class PartitionsChangedEventArgs
{
    public static readonly PartitionsChangedEventArgs Empty = new();
}

internal sealed class PartitioningConfig : IPartitioningConfig
{
    private readonly ILogger _logger;
    private readonly IConfigSection<PartitioningValues> _section;
    private readonly IEventSource<PartitionsChangedEventArgs> _partitionsChanged;

    public PartitioningConfig(
        ILogger<PartitioningConfig> logger,
        IConfigSection<PartitioningValues> section,
        IEventSource<PartitionsChangedEventArgs> partitionsChanged)
    {
        _logger = logger;
        _section = section;
        _partitionsChanged = partitionsChanged;

        _section.ValuesChanged += SectionOnValuesChanged;
    }

    public void Dispose()
    {
        _section.ValuesChanged -= SectionOnValuesChanged;
        _partitionsChanged.Dispose();
    }

    private void SectionOnValuesChanged(object? sender, EventArgs e)
    {
        _partitionsChanged.Publish(PartitionsChangedEventArgs.Empty);
    }

    public int PartitionCount
    {
        get
        {
            EnsureInitialized();
            return _section.Values!.PartitionCount;
        }
    }

    public PartitionRange GetPartitions(string server)
    {
        EnsureInitialized();

        if (!_section.Values!.ServerPartitionMap.TryGetValue(server, out PartitionRange partitions))
        {
            throw new InvalidOperationException($"No partitions configured for server {server}.");
        }

        return partitions;
    }

    public string GetAddress(int partition)
    {
        EnsureInitialized();

        if (!_section.Values!.PartitionServerMap.TryGetValue(partition, out string? server))
        {
            throw new InvalidOperationException($"No server configured for partition {partition}.");
        }
        if (!_section.Values.ServerAddressMap.TryGetValue(server, out string? address))
        {
            throw new InvalidOperationException($"No address configured for server {server}.");
        }

        return address;
    }

    private void EnsureInitialized()
    {
        if (_section.Values == null)
        {
            throw new InvalidOperationException($"Call '{nameof(Initialize)}' to initialize the config.");
        }
    }

    public Task<bool> Initialize(CancellationToken ct)
    {
        return _section.Initialize(ConfigKeys.Partitioning.Section, PrintValues, ct);
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
}
