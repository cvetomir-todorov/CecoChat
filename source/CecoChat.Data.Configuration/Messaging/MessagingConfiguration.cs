using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using CecoChat.Redis;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CecoChat.Data.Configuration.Messaging
{
    public sealed class MessagingConfigurationUsage
    {
        public bool UsePartitionCount { get; set; } = true;

        public bool UseServerAddressByPartition { get; set; } = true;
    }

    public interface IMessagingConfiguration : IDisposable
    {
        Task Initialize(MessagingConfigurationUsage usage);

        int PartitionCount { get; }

        string GetServerAddress(int partition);
    }

    public sealed class MessagingConfiguration : IMessagingConfiguration
    {
        private readonly ILogger _logger;
        private readonly IRedisContext _redisContext;
        private readonly IMessagingConfigurationRepository _repository;
        // TODO: consider using partition ranges and binary search
        private readonly ConcurrentDictionary<int, string> _partitionServerMap;
        private readonly ConcurrentDictionary<string, string> _serverAddressMap;

        public MessagingConfiguration(
            ILogger<MessagingConfiguration> logger,
            IRedisContext redisContext,
            IMessagingConfigurationRepository repository)
        {
            _logger = logger;
            _redisContext = redisContext;
            _repository = repository;

            _partitionServerMap = new();
            _serverAddressMap = new();
        }

        public void Dispose()
        {
            _redisContext.Dispose();
        }

        public int PartitionCount { get; private set; }

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

        public async Task Initialize(MessagingConfigurationUsage usage)
        {
            ISubscriber subscriber = _redisContext.GetSubscriber();

            if (usage.UsePartitionCount)
            {
                ChannelMessageQueue partitionCountMQ = await subscriber.SubscribeAsync($"__keyspace*__:{MessagingKeys.PartitionCount}");
                partitionCountMQ.OnMessage(HandlePartitionCount);
            }

            if (usage.UseServerAddressByPartition)
            {
                ChannelMessageQueue serverPartitionsMQ = await subscriber.SubscribeAsync($"__keyspace*__:{MessagingKeys.ServerPartitions}");
                serverPartitionsMQ.OnMessage(HandleServerPartitions);

                ChannelMessageQueue serverAddressesMQ = await subscriber.SubscribeAsync($"__keyspace*__:{MessagingKeys.ServerAddresses}");
                serverAddressesMQ.OnMessage(HandleServerAddresses);
            }

            await LoadValues(usage);
        }

        private async Task LoadValues(MessagingConfigurationUsage usage)
        {
            try
            {
                _logger.LogInformation("Loading messaging configuration...");

                if (usage.UsePartitionCount)
                {
                    await SetPartitionCount();
                }

                if (usage.UseServerAddressByPartition)
                {
                    await SetServerPartitions(strictlyAdd: true);
                    await SetServerAddresses(strictlyAdd: true);
                }

                _logger.LogInformation("Loading messaging configuration succeeded.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Loading messaging configuration failed.");
            }
        }

        private async Task HandlePartitionCount(ChannelMessage channelMessage)
        {
            try
            {
                _logger.LogInformation("Partition count change. {0} -> {1}.", channelMessage.Channel, channelMessage.Message);

                if (ChannelMessageIs(channelMessage, "set"))
                {
                    await SetPartitionCount();
                }
                if (ChannelMessageIs(channelMessage, "del"))
                {
                    _logger.LogError("Key {0} was deleted.", MessagingKeys.PartitionCount);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error occurred while processing change from {0}.", channelMessage);
            }
        }

        private async Task HandleServerPartitions(ChannelMessage channelMessage)
        {
            try
            {
                _logger.LogInformation("Server partitions change. {0} -> {1}.", channelMessage.Channel, channelMessage.Message);

                if (ChannelMessageIs(channelMessage, "hset", "hdel"))
                {
                    await SetServerPartitions(strictlyAdd: false);
                }
                if (ChannelMessageIs(channelMessage, "del"))
                {
                    _logger.LogError("Key {0} was deleted.", MessagingKeys.ServerPartitions);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error occurred while processing change from {0}.", channelMessage);
            }
        }

        private async Task HandleServerAddresses(ChannelMessage channelMessage)
        {
            try
            {
                _logger.LogInformation("Server addresses change. {0} -> {1}.", channelMessage.Channel, channelMessage.Message);

                if (ChannelMessageIs(channelMessage, "hset", "hdel"))
                {
                    await SetServerAddresses(strictlyAdd: false);
                }
                if (ChannelMessageIs(channelMessage, "del"))
                {
                    _logger.LogError("Key {0} was deleted.", MessagingKeys.ServerAddresses);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error occurred while processing change from {0}.", channelMessage);
            }
        }

        private async Task SetPartitionCount()
        {
            int partitionCount = await _repository.GetPartitionCount();
            PartitionCount = partitionCount;
            _logger.LogInformation("Partition count set to {1}.", partitionCount);
        }

        private async Task SetServerPartitions(bool strictlyAdd)
        {
            await foreach (KeyValuePair<string, PartitionRange> serverPartitions in _repository.GetServerPartitions())
            {
                int successfullySet = 0;

                for (int partition = serverPartitions.Value.Lower; partition <= serverPartitions.Value.Upper; ++partition)
                {
                    if (strictlyAdd)
                    {
                        if (_partitionServerMap.TryAdd(partition, serverPartitions.Key))
                        {
                            successfullySet++;
                        }
                    }
                    else
                    {
                        _partitionServerMap.AddOrUpdate(partition, serverPartitions.Key, (_, _) => serverPartitions.Key);
                        successfullySet = serverPartitions.Value.Length;
                    }
                }

                _logger.LogDebug("Partitions {0} ({1} out of {2}) assigned to server {3}.",
                    serverPartitions.Value, successfullySet, serverPartitions.Value.Length, serverPartitions.Key);
            }
        }

        private async Task SetServerAddresses(bool strictlyAdd)
        {
            await foreach (KeyValuePair<string, string> serverAddress in _repository.GetServerAddresses())
            {
                if (strictlyAdd)
                {
                    if (_serverAddressMap.TryAdd(serverAddress.Key, serverAddress.Value))
                    {
                        _logger.LogDebug("Server {0} assigned address {1}.", serverAddress.Key, serverAddress.Value);
                    }
                }
                else
                {
                    _serverAddressMap.AddOrUpdate(serverAddress.Key, serverAddress.Value, (_, _) => serverAddress.Value);
                    _logger.LogDebug("Server {0} assigned address {1}.", serverAddress.Key, serverAddress.Value);
                }
            }
        }

        private static bool ChannelMessageIs(ChannelMessage channelMessage, params string[] expectedMessages)
        {
            foreach (string expectedMessage in expectedMessages)
            {
                if (string.Equals(channelMessage.Message, expectedMessage, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
