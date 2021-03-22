using System;
using System.Threading.Tasks;
using CecoChat.Redis;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CecoChat.Data.Configuration.History
{
    internal sealed class HistoryConfiguration : IHistoryConfiguration
    {
        private readonly ILogger _logger;
        private readonly IRedisContext _redisContext;
        private readonly IHistoryConfigurationRepository _repository;
        private readonly IConfigurationUtility _configurationUtility;

        private HistoryConfigurationUsage _usage;
        private HistoryConfigurationValues _values;
        private HistoryConfigurationValidator _validator;

        public HistoryConfiguration(
            ILogger<HistoryConfiguration> logger,
            IRedisContext redisContext,
            IHistoryConfigurationRepository repository,
            IConfigurationUtility configurationUtility)
        {
            _logger = logger;
            _redisContext = redisContext;
            _repository = repository;
            _configurationUtility = configurationUtility;
        }

        public string ServerAddress => _values.ServerAddress;

        public int UserMessageCount => _values.UserMessageCount;

        public int DialogMessageCount => _values.DialogMessageCount;

        public async Task Initialize(HistoryConfigurationUsage usage)
        {
            try
            {
                _usage = usage;
                await SubscribeForChanges(usage);

                _validator = new HistoryConfigurationValidator(usage);
                await LoadValidateValues(usage, _validator);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Initializing history configuration failed.");
            }
        }

        private async Task SubscribeForChanges(HistoryConfigurationUsage usage)
        {
            ISubscriber subscriber = _redisContext.GetSubscriber();

            if (usage.UseServerAddress)
            {
                ChannelMessageQueue serverAddressMQ = await subscriber.SubscribeAsync($"notify:{HistoryKeys.ServerAddress}");
                serverAddressMQ.OnMessage(channelMessage => _configurationUtility.HandleChange(channelMessage, HandleServerAddress));
                _logger.LogInformation("Subscribed for changes about {0} from channel {1}.",
                    HistoryKeys.ServerAddress, serverAddressMQ.Channel);
            }
            if (usage.UseMessageCount)
            {
                ChannelMessageQueue userMessageCountMQ = await subscriber.SubscribeAsync($"notify:{HistoryKeys.MessageCount}");
                userMessageCountMQ.OnMessage(channelMessage => _configurationUtility.HandleChange(channelMessage, HandleMessageCount));
                _logger.LogInformation("Subscribed for changes about {0}, {1} from channel {2}.",
                    HistoryKeys.UserMessageCount, HistoryKeys.DialogMessageCount, userMessageCountMQ.Channel);
            }
        }

        private async Task HandleServerAddress(ChannelMessage channelMessage)
        {
            if (_usage.UseServerAddress)
            {
                await LoadValidateValues(_usage, _validator);
            }
        }

        private async Task HandleMessageCount(ChannelMessage channelMessage)
        {
            if (_usage.UseMessageCount)
            {
                await LoadValidateValues(_usage, _validator);
            }
        }

        private async Task LoadValidateValues(HistoryConfigurationUsage usage, HistoryConfigurationValidator validator)
        {
            HistoryConfigurationValues values = await _repository.GetValues(usage);
            _logger.LogInformation("Loading history configuration succeeded.");

            if (_configurationUtility.ValidateValues("history", values, validator))
            {
                _values = values;
                PrintValues(usage, values);
            }
        }

        private void PrintValues(HistoryConfigurationUsage usage, HistoryConfigurationValues values)
        {
            if (usage.UseServerAddress)
            {
                _logger.LogInformation("Server address set to {0}.", values.ServerAddress);
            }
            if (usage.UseMessageCount)
            {
                _logger.LogInformation("User message count set to {0}.", values.UserMessageCount);
                _logger.LogInformation("Dialog message count set to {0}.", values.DialogMessageCount);
            }
        }
    }
}
