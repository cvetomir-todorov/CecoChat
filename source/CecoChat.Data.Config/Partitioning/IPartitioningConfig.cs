using System;
using System.Threading.Tasks;
using CecoChat.Kafka;

namespace CecoChat.Data.Config.Partitioning;

public sealed class PartitioningConfigUsage
{
    public bool UseServerPartitions { get; set; }

    public string ServerPartitionChangesToWatch { get; set; }

    public bool UseServerAddresses { get; set; }
}

public interface IPartitioningConfig : IDisposable
{
    Task Initialize(PartitioningConfigUsage usage);

    int PartitionCount { get; }

    PartitionRange GetServerPartitions(string server);

    string GetServerAddress(int partition);
}

public sealed class PartitionsChangedEventData
{
    public int PartitionCount { get; init; }

    public PartitionRange Partitions { get; init; }
}