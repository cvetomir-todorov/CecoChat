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
        private readonly IConfigurationUtility _configurationUtility;

        private readonly ConcurrentDictionary<int, string> _partitionServerMap;
        private readonly ConcurrentDictionary<string, string> _serverAddressMap;

        public MessagingConfiguration(
            ILogger<MessagingConfiguration> logger,
            IRedisContext redisContext,
            IMessagingConfigurationRepository repository,
            IConfigurationUtility configurationUtility)
        {
            _logger = logger;
            _redisContext = redisContext;
            _repository = repository;
            _configurationUtility = configurationUtility;

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
            try
            {
                ISubscriber subscriber = _redisContext.GetSubscriber();

                if (usage.UsePartitionCount)
                {
                    ChannelMessageQueue partitionCountMQ = await subscriber.SubscribeAsync($"__keyspace*__:{MessagingKeys.PartitionCount}");
                    partitionCountMQ.OnMessage(channelMessage => _configurationUtility.HandleChange(channelMessage, HandlePartitionCount));
                    _logger.LogInformation("Subscribed for changes about {0}.", MessagingKeys.PartitionCount);
                }

                if (usage.UseServerAddressByPartition)
                {
                    ChannelMessageQueue serverPartitionsMQ = await subscriber.SubscribeAsync($"__keyspace*__:{MessagingKeys.ServerPartitions}");
                    serverPartitionsMQ.OnMessage(channelMessage => _configurationUtility.HandleChange(channelMessage, HandleServerPartitions));
                    _logger.LogInformation("Subscribed for changes about {0}.", MessagingKeys.ServerPartitions);

                    ChannelMessageQueue serverAddressesMQ = await subscriber.SubscribeAsync($"__keyspace*__:{MessagingKeys.ServerAddresses}");
                    serverAddressesMQ.OnMessage(channelMessage => _configurationUtility.HandleChange(channelMessage, HandleServerAddresses));
                    _logger.LogInformation("Subscribed for changes about {0}.", MessagingKeys.ServerAddresses);
                }

                await LoadValues(usage);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Initializing messaging configuration failed.");
            }
        }

        private async Task LoadValues(MessagingConfigurationUsage usage)
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

        private async Task HandlePartitionCount(ChannelMessage channelMessage)
        {
            if (_configurationUtility.ChannelMessageIs(channelMessage, "set"))
            {
                await SetPartitionCount();
            }
            if (_configurationUtility.ChannelMessageIs(channelMessage, "del"))
            {
                _logger.LogError("Key {0} was deleted.", MessagingKeys.PartitionCount);
            }
        }

        private async Task HandleServerPartitions(ChannelMessage channelMessage)
        {
            if (_configurationUtility.ChannelMessageIs(channelMessage, "hset", "hdel"))
            {
                await SetServerPartitions(strictlyAdd: false);
            }
            if (_configurationUtility.ChannelMessageIs(channelMessage, "del"))
            {
                _logger.LogError("Key {0} was deleted.", MessagingKeys.ServerPartitions);
            }
        }

        private async Task HandleServerAddresses(ChannelMessage channelMessage)
        {
            if (_configurationUtility.ChannelMessageIs(channelMessage, "hset", "hdel"))
            {
                await SetServerAddresses(strictlyAdd: false);
            }
            if (_configurationUtility.ChannelMessageIs(channelMessage, "del"))
            {
                _logger.LogError("Key {0} was deleted.", MessagingKeys.ServerAddresses);
            }
        }

        private async Task SetPartitionCount()
        {
            RedisValueResult<int> partitionCountResult = await _repository.GetPartitionCount();
            if (partitionCountResult.IsSuccess)
            {
                PartitionCount = partitionCountResult.Value;
                _logger.LogInformation("Partition count set to {0}.", partitionCountResult.Value);
            }
            else
            {
                _logger.LogError("Partition count is invalid.");
            }
        }

        private async Task SetServerPartitions(bool strictlyAdd)
        {
            await foreach (RedisValueResult<KeyValuePair<string, PartitionRange>> serverPartitionsResult in _repository.GetServerPartitions())
            {
                if (serverPartitionsResult.IsSuccess)
                {
                    string server = serverPartitionsResult.Value.Key;
                    PartitionRange partitions = serverPartitionsResult.Value.Value;
                    int successfullySet = strictlyAdd ? 0 : partitions.Length;

                    for (int partition = partitions.Lower; partition <= partitions.Upper; ++partition)
                    {
                        if (strictlyAdd)
                        {
                            if (_partitionServerMap.TryAdd(partition, server))
                            {
                                successfullySet++;
                            }
                        }
                        else
                        {
                            _partitionServerMap.AddOrUpdate(partition, server, (_, _) => server);
                        }
                    }

                    _logger.LogInformation("Partitions {0} ({1} out of {2}) assigned to server {3}.",
                        partitions, successfullySet, partitions.Length, server);
                }
                else
                {
                    _logger.LogError("Server partitions is invalid.");
                }
            }
        }

        private async Task SetServerAddresses(bool strictlyAdd)
        {
            await foreach (RedisValueResult<KeyValuePair<string, string>> serverAddressResult in _repository.GetServerAddresses())
            {
                if (serverAddressResult.IsSuccess)
                {
                    string server = serverAddressResult.Value.Key;
                    string address = serverAddressResult.Value.Value;

                    if (strictlyAdd)
                    {
                        if (_serverAddressMap.TryAdd(server, address))
                        {
                            _logger.LogInformation("Server {0} assigned address {1}.", server, address);
                        }
                    }
                    else
                    {
                        _serverAddressMap.AddOrUpdate(server, address, (_, _) => address);
                        _logger.LogInformation("Server {0} assigned address {1}.", server, address);
                    }
                }
                else
                {
                    _logger.LogError("Server address is invalid.");
                }
            }
        }
    }
}
