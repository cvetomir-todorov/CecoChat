using CecoChat.Kafka;

namespace CecoChat.Data.Config.Partitioning;

public sealed class PartitioningConfigUsage
{
    public bool UsePartitions { get; init; }

    public string ServerToWatch { get; init; } = string.Empty;

    public bool UseAddresses { get; init; }
}

public interface IPartitioningConfig : IDisposable
{
    Task Initialize(PartitioningConfigUsage usage);

    int PartitionCount { get; }

    PartitionRange GetPartitions(string server);

    string GetAddress(int partition);
}

public sealed class PartitionsChangedEventData
{
    public int PartitionCount { get; init; }

    public PartitionRange Partitions { get; init; }
}
