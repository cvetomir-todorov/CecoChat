using System;
using System.Collections.Concurrent;
using CecoChat.Kafka;

namespace CecoChat.Data.Configuration.Messaging
{
    internal sealed class MessagingConfigurationState
    {
        private readonly ConcurrentDictionary<int, string> _partitionServerMap;
        private readonly ConcurrentDictionary<string, string> _serverAddressMap;
        private readonly ConcurrentDictionary<string, PartitionRange> _serverPartitionsMap;

        public MessagingConfigurationState()
        {
            _partitionServerMap = new();
            _serverAddressMap = new();
            _serverPartitionsMap = new();
        }

        public string GetServerAddress(int partition)
        {
            if (!_partitionServerMap.TryGetValue(partition, out string server))
            {
                throw new InvalidOperationException($"No server configured for partition {partition}.");
            }
            if (!_serverAddressMap.TryGetValue(server, out string serverAddress))
            {
                throw new InvalidOperationException($"No server address configured for server {server}.");
            }

            return serverAddress;
        }

        public int SetServerForPartitions(string server, PartitionRange partitions, bool strictlyAdd)
        {
            int intPartitionsSet = strictlyAdd ? 0 : partitions.Length;

            for (int partition = partitions.Lower; partition <= partitions.Upper; ++partition)
            {
                if (strictlyAdd)
                {
                    if (_partitionServerMap.TryAdd(partition, server))
                    {
                        intPartitionsSet++;
                    }
                }
                else
                {
                    _partitionServerMap.AddOrUpdate(partition, server, (_, _) => server);
                }
            }

            return intPartitionsSet;
        }

        public bool SetServerAddress(string server, string address, bool strictlyAdd)
        {
            bool isSet = true;

            if (strictlyAdd)
            {
                isSet = _serverAddressMap.TryAdd(server, address);
            }
            else
            {
                _serverAddressMap.AddOrUpdate(server, address, (_, _) => address);
            }

            return isSet;
        }

        public PartitionRange GetPartitionsForServer(string server)
        {
            if (!_serverPartitionsMap.TryGetValue(server, out PartitionRange partitions))
            {
                throw new InvalidOperationException($"No partitions configured for server {server}.");
            }

            return partitions;
        }

        public bool SetPartitionsForServer(string server, PartitionRange partitions, bool strictlyAdd)
        {
            bool isSet = true;

            if (strictlyAdd)
            {
                isSet = _serverPartitionsMap.TryAdd(server, partitions);
            }
            else
            {
                _serverPartitionsMap.AddOrUpdate(server, partitions, (_, _) => partitions);
            }

            return isSet;
        }
    }
}
