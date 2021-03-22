using System.Collections.Generic;
using CecoChat.Kafka;

namespace CecoChat.Data.Configuration.Partitioning
{
    internal sealed class PartitioningConfigurationValues
    {
        public PartitioningConfigurationValues()
        {
            PartitionServerMap = new Dictionary<int, string>();
            ServerPartitionsMap = new Dictionary<string, PartitionRange>();
            ServerAddressMap = new Dictionary<string, string>();
        }

        public int PartitionCount { get; set; }

        public IDictionary<int, string> PartitionServerMap { get; }

        public IDictionary<string, PartitionRange> ServerPartitionsMap { get; }

        public IDictionary<string, string> ServerAddressMap { get; }
    }
}
