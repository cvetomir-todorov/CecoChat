using CecoChat.Kafka;

namespace CecoChat.Data.Config.Partitioning;

public interface IPartitioningConfig : IDisposable
{
    Task<bool> Initialize();

    int PartitionCount { get; }

    PartitionRange GetPartitions(string server);

    string GetAddress(int partition);
}
