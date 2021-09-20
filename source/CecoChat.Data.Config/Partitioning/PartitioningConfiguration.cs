using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CecoChat.Events;
using CecoChat.Kafka;
using CecoChat.Redis;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CecoChat.Data.Config.Partitioning
{
    internal sealed class PartitioningConfiguration : IPartitioningConfiguration
    {
        private readonly ILogger _logger;
        private readonly IRedisContext _redisContext;
        private readonly IPartitioningConfigurationRepository _repository;
        private readonly IConfigurationUtility _configurationUtility;
        private readonly IEventSource<PartitionsChangedEventData> _partitionsChanged;

        private PartitioningConfigurationUsage _usage;
        private PartitioningConfigurationValues _values;
        private PartitioningConfigurationValidator _validator;

        public PartitioningConfiguration(
            ILogger<PartitioningConfiguration> logger,
            IRedisContext redisContext,
            IPartitioningConfigurationRepository repository,
            IConfigurationUtility configurationUtility,
            IEventSource<PartitionsChangedEventData> partitionsChanged)
        {
            _logger = logger;
            _redisContext = redisContext;
            _repository = repository;
            _configurationUtility = configurationUtility;
            _partitionsChanged = partitionsChanged;
        }

        public void Dispose()
        {
            _redisContext.Dispose();
        }

        public int PartitionCount => _values.PartitionCount;

        public PartitionRange GetServerPartitions(string server)
        {
            if (!_values.ServerPartitionsMap.TryGetValue(server, out PartitionRange partitions))
            {
                throw new InvalidOperationException($"No partitions configured for server {server}.");
            }

            return partitions;
        }

        public string GetServerAddress(int partition)
        {
            if (!_values.PartitionServerMap.TryGetValue(partition, out string server))
            {
                throw new InvalidOperationException($"No server configured for partition {partition}.");
            }
            if (!_values.ServerAddressMap.TryGetValue(server, out string address))
            {
                throw new InvalidOperationException($"No address configured for server {server}.");
            }

            return address;
        }

        public async Task Initialize(PartitioningConfigurationUsage usage)
        {
            try
            {
                _usage = usage;
                await SubscribeForChanges(usage);

                _validator = new PartitioningConfigurationValidator(usage);
                await LoadValidateValues(usage, _validator);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Initializing partitioning configuration failed.");
            }
        }

        private async Task SubscribeForChanges(PartitioningConfigurationUsage usage)
        {
            ISubscriber subscriber = _redisContext.GetSubscriber();

            if (usage.UseServerPartitions || usage.UseServerAddresses)
            {
                ChannelMessageQueue partitionsMQ = await subscriber.SubscribeAsync($"notify:{PartitioningKeys.ServerPartitions}");
                partitionsMQ.OnMessage(channelMessage => _configurationUtility.HandleChange(channelMessage, HandleServerPartitions));
                _logger.LogInformation("Subscribed for changes about {0}, {1} from channel {2}.",
                    PartitioningKeys.PartitionCount, PartitioningKeys.ServerPartitions, partitionsMQ.Channel);
            }
            if (usage.UseServerAddresses)
            {
                ChannelMessageQueue serverAddressesMQ = await subscriber.SubscribeAsync($"notify:{PartitioningKeys.ServerAddresses}");
                serverAddressesMQ.OnMessage(channelMessage => _configurationUtility.HandleChange(channelMessage, HandleServerAddresses));
                _logger.LogInformation("Subscribed for changes about {0} from channel {1}.",
                    PartitioningKeys.ServerAddresses, serverAddressesMQ.Channel);
            }
        }

        private async Task HandleServerPartitions(ChannelMessage channelMessage)
        {
            if (!_usage.UseServerPartitions && !_usage.UseServerAddresses)
            {
                return;
            }

            bool areValid = await LoadValidateValues(_usage, _validator);
            if (areValid && !string.IsNullOrWhiteSpace(_usage.ServerPartitionChangesToWatch))
            {
                PartitioningConfigurationValues values = _values;
                if (values.ServerPartitionsMap.TryGetValue(_usage.ServerPartitionChangesToWatch, out PartitionRange partitions))
                {
                    PartitionsChangedEventData eventData = new()
                    {
                        PartitionCount = values.PartitionCount,
                        Partitions = partitions
                    };

                    _partitionsChanged.Publish(eventData);
                }
                else
                {
                    _logger.LogError("After server partition changes there are no partitions for watched server {0}.",
                        _usage.ServerPartitionChangesToWatch);
                }
            }
        }

        private async Task HandleServerAddresses(ChannelMessage channelMessage)
        {
            if (_usage.UseServerAddresses)
            {
                await LoadValidateValues(_usage, _validator);
            }
        }

        private async Task<bool> LoadValidateValues(PartitioningConfigurationUsage usage, PartitioningConfigurationValidator validator)
        {
            PartitioningConfigurationValues values = await _repository.GetValues(usage);
            _logger.LogInformation("Loading partitioning configuration succeeded.");

            bool areValid = _configurationUtility.ValidateValues("partitioning", values, validator);
            if (areValid)
            {
                _values = values;
                PrintValues(usage, values);
            }

            return areValid;
        }

        private void PrintValues(PartitioningConfigurationUsage usage, PartitioningConfigurationValues values)
        {
            _logger.LogInformation("Partition count set to {0}.", values.PartitionCount);

            if (usage.UseServerPartitions || usage.UseServerAddresses)
            {
                foreach (KeyValuePair<string, PartitionRange> pair in values.ServerPartitionsMap)
                {
                    string server = pair.Key;
                    PartitionRange partitions = pair.Value;
                    _logger.LogInformation("Partitions {0} are assigned to server {1}.", partitions, server);
                }
            }
            if (usage.UseServerAddresses)
            {
                foreach (KeyValuePair<string, string> pair in values.ServerAddressMap)
                {
                    string server = pair.Key;
                    string address = pair.Value;
                    _logger.LogInformation("Address {0} is assigned to server {1}.", address, server);
                }
            }
        }
    }
}
