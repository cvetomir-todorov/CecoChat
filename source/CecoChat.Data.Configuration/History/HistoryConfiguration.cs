using System;
using System.Threading.Tasks;
using CecoChat.Redis;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CecoChat.Data.Configuration.History
{
    public sealed class HistoryConfigurationUsage
    {
        public bool UseServerAddress { get; set; }

        public bool UseUserMessageCount { get; set; }

        public bool UseDialogMessageCount { get; set; }
    }

    public interface IHistoryConfiguration
    {
        Task Initialize(HistoryConfigurationUsage usage);

        string ServerAddress { get; }

        int UserMessageCount { get; }

        int DialogMessageCount { get; }
    }

    public sealed class HistoryConfiguration : IHistoryConfiguration
    {
        private readonly ILogger _logger;
        private readonly IRedisContext _redisContext;
        private readonly IHistoryConfigurationRepository _repository;
        private readonly IConfigurationUtility _configurationUtility;

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

        public string ServerAddress { get; private set; }

        public int UserMessageCount { get; private set; }

        public int DialogMessageCount { get; private set; }

        public async Task Initialize(HistoryConfigurationUsage usage)
        {
            try
            {
                ISubscriber subscriber = _redisContext.GetSubscriber();

                if (usage.UseServerAddress)
                {
                    ChannelMessageQueue serverAddressMQ = await subscriber.SubscribeAsync($"__keyspace*__:{HistoryKeys.ServerAddress}");
                    serverAddressMQ.OnMessage(channelMessage => _configurationUtility.HandleChange(channelMessage, HandleServerAddress));
                    _logger.LogInformation("Subscribed for changes about {0}.", HistoryKeys.ServerAddress);
                }
                if (usage.UseUserMessageCount)
                {
                    ChannelMessageQueue userMessageCountMQ = await subscriber.SubscribeAsync($"__keyspace*__:{HistoryKeys.UserMessageCount}");
                    userMessageCountMQ.OnMessage(channelMessage => _configurationUtility.HandleChange(channelMessage, HandleUserMessageCount));
                    _logger.LogInformation("Subscribed for changes about {0}.", HistoryKeys.UserMessageCount);
                }
                if (usage.UseDialogMessageCount)
                {
                    ChannelMessageQueue dialogMessageCountMQ = await subscriber.SubscribeAsync($"__keyspace*__:{HistoryKeys.DialogMessageCount}");
                    dialogMessageCountMQ.OnMessage(channelMessage => _configurationUtility.HandleChange(channelMessage, HandleDialogMessageCount));
                    _logger.LogInformation("Subscribed for changes about {0}.", HistoryKeys.DialogMessageCount);
                }

                await LoadValues(usage);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Initializing history configuration failed.");
            }
        }

        private async Task LoadValues(HistoryConfigurationUsage usage)
        {
            _logger.LogInformation("Loading history configuration...");

            if (usage.UseServerAddress)
            {
                await SetServerAddress();
            }
            if (usage.UseUserMessageCount)
            {
                await SetUserMessageCount();
            }
            if (usage.UseDialogMessageCount)
            {
                await SetDialogMessageCount();
            }

            _logger.LogInformation("Loading history configuration succeeded.");
        }

        private async Task HandleServerAddress(ChannelMessage channelMessage)
        {
            if (_configurationUtility.ChannelMessageIs(channelMessage, "set"))
            {
                await SetServerAddress();
            }
            else if (_configurationUtility.ChannelMessageIs(channelMessage, "del"))
            {
                _logger.LogError("Key {0} was deleted.", HistoryKeys.ServerAddress);
            }
        }

        private async Task HandleUserMessageCount(ChannelMessage channelMessage)
        {
            if (_configurationUtility.ChannelMessageIs(channelMessage, "set"))
            {
                await SetUserMessageCount();
            }
            else if (_configurationUtility.ChannelMessageIs(channelMessage, "del"))
            {
                _logger.LogError("Key {0} was deleted.", HistoryKeys.UserMessageCount);
            }
        }

        private async Task HandleDialogMessageCount(ChannelMessage channelMessage)
        {
            if (_configurationUtility.ChannelMessageIs(channelMessage, "set"))
            {
                await SetDialogMessageCount();
            }
            else if (_configurationUtility.ChannelMessageIs(channelMessage, "del"))
            {
                _logger.LogError("Key {0} was deleted.", HistoryKeys.DialogMessageCount);
            }
        }

        private async Task SetServerAddress()
        {
            RedisValueResult<string> result = await _repository.GetServerAddress();
            if (result.IsSuccess)
            {
                ServerAddress = result.Value;
                _logger.LogInformation("Server address set to {0}.", result.Value);
            }
            else
            {
                _logger.LogError("Server address is invalid.");
            }
        }

        private async Task SetUserMessageCount()
        {
            RedisValueResult<int> result = await _repository.GetUserMessageCount();
            if (result.IsSuccess)
            {
                UserMessageCount = result.Value;
                _logger.LogInformation("User message count set to {0}.", result.Value);
            }
            else
            {
                _logger.LogError("User message count is invalid.");
            }
        }

        private async Task SetDialogMessageCount()
        {
            RedisValueResult<int> result = await _repository.GetDialogMessageCount();
            if (result.IsSuccess)
            {
                DialogMessageCount = result.Value;
                _logger.LogInformation("Dialog message count set to {0}.", result.Value);
            }
            else
            {
                _logger.LogError("Dialog message count is invalid.");
            }
        }
    }
}
