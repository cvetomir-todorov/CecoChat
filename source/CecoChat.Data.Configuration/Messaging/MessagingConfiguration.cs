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
        public bool UsePartitions { get; set; }

        public string ServerForWhichToUsePartitions { get; set; }

        public bool UseServerAddressByPartition { get; set; }
    }

    public interface IMessagingConfiguration : IDisposable
    {
        Task Initialize(MessagingConfigurationUsage usage);

        int PartitionCount { get; }

        PartitionRange GetServerPartitions(string server);

        string GetServerAddress(int partition);
    }

    public sealed class PartitionsChangedEventData
    {
        public int PartitionCount { get; init; }

        public PartitionRange Partitions { get; init; }
    }

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

        public int PartitionCount => _state.PartitionCount;

        public PartitionRange GetServerPartitions(string server)
        {
            return _state.GetPartitionsForServer(server);
        }

        public string GetServerAddress(int partition)
        {
            return _state.GetServerAddress(partition);
        }

        public async Task Initialize(MessagingConfigurationUsage usage)
        {
            try
            {
                _usage = usage;
                ISubscriber subscriber = _redisContext.GetSubscriber();

                if (usage.UsePartitions || usage.UseServerAddressByPartition)
                {
                    ChannelMessageQueue partitionsMQ = await subscriber.SubscribeAsync($"notify:{MessagingKeys.Partitions}");
                    partitionsMQ.OnMessage(channelMessage => _configurationUtility.HandleChange(channelMessage, HandlePartitions));
                    _logger.LogInformation("Subscribed for changes about {0}, {1} from channel {2}.",
                        MessagingKeys.PartitionCount, MessagingKeys.ServerPartitions, partitionsMQ.Channel);
                }
                if (usage.UseServerAddressByPartition)
                {
                    ChannelMessageQueue serverAddressesMQ = await subscriber.SubscribeAsync($"notify:{MessagingKeys.ServerAddresses}");
                    serverAddressesMQ.OnMessage(channelMessage => _configurationUtility.HandleChange(channelMessage, HandleServerAddresses));
                    _logger.LogInformation("Subscribed for changes about {0} from channel {1}.",
                        MessagingKeys.ServerAddresses, serverAddressesMQ.Channel);
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

            if (usage.UsePartitions || usage.UseServerAddressByPartition)
            {
                await SetPartitionCount();
                await SetServerPartitions(strictlyAdd: true);
            }
            if (usage.UseServerAddressByPartition)
            {
                await SetServerAddresses(strictlyAdd: true);
            }

            _logger.LogInformation("Loading messaging configuration succeeded.");
        }

        private async Task HandlePartitions(ChannelMessage channelMessage)
        {
            await SetPartitionCount();
            await SetServerPartitions(strictlyAdd: false);

            PartitionRange partitions = PartitionRange.Empty;
            if (!string.IsNullOrWhiteSpace(_usage.ServerForWhichToUsePartitions))
            {
                partitions = _state.GetPartitionsForServer(_usage.ServerForWhichToUsePartitions);
            }

            PartitionsChangedEventData eventData = new()
            {
                PartitionCount = _state.PartitionCount,
                Partitions = partitions
            };

            _partitionsChanged.Publish(eventData);
        }

        private async Task HandleServerAddresses(ChannelMessage channelMessage)
        {
            await SetServerAddresses(strictlyAdd: false);
        }

        private async Task SetPartitionCount()
        {
            RedisValueResult<int> result = await _repository.GetPartitionCount();
            if (result.IsSuccess)
            {
                _state.PartitionCount = result.Value;
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
                    if (_usage.UsePartitions)
                    {
                        if (_state.SetPartitionsForServer(server, partitions, strictlyAdd))
                        {
                            _logger.LogInformation("Partitions {0} are assigned to server {1}.", partitions, server);
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
                        _logger.LogInformation("Address {0} is assigned to server {1}.", address, server);
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
