using Common.Kafka;

namespace CecoChat.Config.Sections.Partitioning;

internal sealed class PartitioningValues
{
    public PartitioningValues()
    {
        PartitionServerMap = new Dictionary<int, string>();
        ServerPartitionMap = new Dictionary<string, PartitionRange>();
        ServerAddressMap = new Dictionary<string, string>();
    }

    public int PartitionCount { get; set; }

    public IDictionary<int, string> PartitionServerMap { get; }

    public IDictionary<string, PartitionRange> ServerPartitionMap { get; }

    public IDictionary<string, string> ServerAddressMap { get; }
}
