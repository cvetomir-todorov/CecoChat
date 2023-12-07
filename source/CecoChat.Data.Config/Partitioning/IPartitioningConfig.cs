using CecoChat.Kafka;

namespace CecoChat.Data.Config.Partitioning;

public sealed class PartitioningConfigUsage
{
    public string ServerToWatch { get; init; } = string.Empty;
}

public interface IPartitioningConfig : IDisposable
{
    Task<bool> Initialize(PartitioningConfigUsage usage);

    int PartitionCount { get; }

    PartitionRange GetPartitions(string server);

    string GetAddress(int partition);
}

public sealed class PartitionsChangedEventData
{
    public int PartitionCount { get; init; }

    public PartitionRange Partitions { get; init; }
}
