using System;
using System.Threading.Tasks;
using CecoChat.Redis;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CecoChat.Data.Configuration.History
{
    public sealed class HistoryConfigurationUsage
    {
        public bool UseServerAddress { get; set; } = true;
    }

    public interface IHistoryConfiguration
    {
        Task Initialize(HistoryConfigurationUsage usage);

        string ServerAddress { get; }
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

            _logger.LogInformation("Loading history configuration succeeded.");
        }

        private async Task HandleServerAddress(ChannelMessage channelMessage)
        {
            if (_configurationUtility.ChannelMessageIs(channelMessage, "set"))
            {
                await SetServerAddress();
            }
            if (_configurationUtility.ChannelMessageIs(channelMessage, "del"))
            {
                _logger.LogError("Key {0} was deleted.", HistoryKeys.ServerAddress);
            }
        }

        private async Task SetServerAddress()
        {
            string serverAddress = await _repository.GetServerAddress();
            ServerAddress = serverAddress;
            _logger.LogInformation("Server address set to {1}.", serverAddress);
        }
    }
}
