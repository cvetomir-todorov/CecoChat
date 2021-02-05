using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CecoChat.Events;
using CecoChat.Kafka;
using CecoChat.Redis;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CecoChat.Data.Configuration.Messaging
{
    public sealed class MessagingConfigurationUsage
    {
        public bool UsePartitionCount { get; set; }

        public bool UseServerAddressByPartition { get; set; }

        public bool UseServerPartitions { get; set; }
    }

    public interface IMessagingConfiguration : IDisposable
    {
        Task Initialize(MessagingConfigurationUsage usage);

        int PartitionCount { get; }

        string GetServerAddress(int partition);

        PartitionRange GetServerPartitions(string server);
    }

    public sealed class PartitionsChangedEventData
    {}

    public sealed class MessagingConfiguration : IMessagingConfiguration
    {
        private readonly ILogger _logger;
        private readonly IRedisContext _redisContext;
        private readonly IMessagingConfigurationRepository _repository;
        private readonly IConfigurationUtility _configurationUtility;
        private readonly IEventSource<PartitionsChangedEventData> _partitionsChanged;

        private readonly MessagingConfigurationState _state;
        private MessagingConfigurationUsage _usage;

        public MessagingConfiguration(
            ILogger<MessagingConfiguration> logger,
            IRedisContext redisContext,
            IMessagingConfigurationRepository repository,
            IConfigurationUtility configurationUtility,
            IEventSource<PartitionsChangedEventData> partitionsChanged)
        {
            _logger = logger;
            _redisContext = redisContext;
            _repository = repository;
            _configurationUtility = configurationUtility;
            _partitionsChanged = partitionsChanged;

            _state = new MessagingConfigurationState();
        }

        public void Dispose()
        {
            _redisContext.Dispose();
        }

        public int PartitionCount { get; private set; }

        public string GetServerAddress(int partition)
        {
            return _state.GetServerAddress(partition);
        }

        public PartitionRange GetServerPartitions(string server)
        {
            return _state.GetPartitionsForServer(server);
        }

        public async Task Initialize(MessagingConfigurationUsage usage)
        {
            try
            {
                _usage = usage;
                ISubscriber subscriber = _redisContext.GetSubscriber();

                if (usage.UsePartitionCount)
                {
                    // TODO: switch to custom pub/sub instead of keyspace notifications which don't work in a cluster
                    // TODO: merge the partition change events and include the data inside the event data class

                    ChannelMessageQueue partitionCountMQ = await subscriber.SubscribeAsync($"__keyspace*__:{MessagingKeys.PartitionCount}");
                    partitionCountMQ.OnMessage(channelMessage => _configurationUtility.HandleChange(channelMessage, HandlePartitionCount));
                    _logger.LogInformation("Subscribed for changes about {0}.", MessagingKeys.PartitionCount);
                }
                if (usage.UseServerAddressByPartition || usage.UseServerPartitions)
                {
                    ChannelMessageQueue serverPartitionsMQ = await subscriber.SubscribeAsync($"__keyspace*__:{MessagingKeys.ServerPartitions}");
                    serverPartitionsMQ.OnMessage(channelMessage => _configurationUtility.HandleChange(channelMessage, HandleServerPartitions));
                    _logger.LogInformation("Subscribed for changes about {0}.", MessagingKeys.ServerPartitions);
                }
                if (usage.UseServerAddressByPartition)
                {
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
            if (usage.UseServerAddressByPartition || usage.UseServerPartitions)
            {
                await SetServerPartitions(strictlyAdd: true);
            }
            if (usage.UseServerAddressByPartition)
            {
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
            else if (_configurationUtility.ChannelMessageIs(channelMessage, "del"))
            {
                _logger.LogError("Key {0} was deleted.", MessagingKeys.PartitionCount);
            }

            _partitionsChanged.Publish(new());
        }

        private async Task HandleServerPartitions(ChannelMessage channelMessage)
        {
            if (_configurationUtility.ChannelMessageIs(channelMessage, "hset", "hdel"))
            {
                await SetServerPartitions(strictlyAdd: false);
            }
            else if (_configurationUtility.ChannelMessageIs(channelMessage, "del"))
            {
                _logger.LogError("Key {0} was deleted.", MessagingKeys.ServerPartitions);
            }

            _partitionsChanged.Publish(new());
        }

        private async Task HandleServerAddresses(ChannelMessage channelMessage)
        {
            if (_configurationUtility.ChannelMessageIs(channelMessage, "hset", "hdel"))
            {
                await SetServerAddresses(strictlyAdd: false);
            }
            else if (_configurationUtility.ChannelMessageIs(channelMessage, "del"))
            {
                _logger.LogError("Key {0} was deleted.", MessagingKeys.ServerAddresses);
            }
        }

        private async Task SetPartitionCount()
        {
            RedisValueResult<int> result = await _repository.GetPartitionCount();
            if (result.IsSuccess)
            {
                PartitionCount = result.Value;
                _logger.LogInformation("Partition count set to {0}.", result.Value);
            }
            else
            {
                _logger.LogError("Partition count is invalid.");
            }
        }

        private async Task SetServerPartitions(bool strictlyAdd)
        {
            await foreach (RedisValueResult<KeyValuePair<string, PartitionRange>> result in _repository.GetServerPartitions())
            {
                if (result.IsSuccess)
                {
                    string server = result.Value.Key;
                    PartitionRange partitions = result.Value.Value;

                    if (_usage.UseServerAddressByPartition)
                    {
                        int partitionsSet = _state.SetServerForPartitions(server, partitions, strictlyAdd);
                        _logger.LogInformation("Server {0} is assigned to partitions {1} ({2} out of {3}).",
                            server, partitions, partitionsSet, partitions.Length);
                    }
                    if (_usage.UseServerPartitions)
                    {
                        if (_state.SetPartitionsForServer(server, partitions, strictlyAdd))
                        {
                            _logger.LogInformation("Server {0} assigned partitions {1}.", server, partitions);
                        }
                    }
                }
                else
                {
                    _logger.LogError("Server partitions are invalid.");
                }
            }
        }

        private async Task SetServerAddresses(bool strictlyAdd)
        {
            await foreach (RedisValueResult<KeyValuePair<string, string>> result in _repository.GetServerAddresses())
            {
                if (result.IsSuccess)
                {
                    string server = result.Value.Key;
                    string address = result.Value.Value;

                    if (_state.SetServerAddress(server, address, strictlyAdd))
                    {
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
