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

        public HistoryConfiguration(
            ILogger<HistoryConfiguration> logger,
            IRedisContext redisContext,
            IHistoryConfigurationRepository repository)
        {
            _logger = logger;
            _redisContext = redisContext;
            _repository = repository;
        }

        public string ServerAddress { get; private set; }

        public async Task Initialize(HistoryConfigurationUsage usage)
        {
            ISubscriber subscriber = _redisContext.GetSubscriber();

            if (usage.UseServerAddress)
            {
                ChannelMessageQueue serverAddressMQ = await subscriber.SubscribeAsync($"__keyspace*__:{HistoryKeys.ServerAddress}");
                serverAddressMQ.OnMessage(HandleServerAddress);
            }

            await LoadValues(usage);
        }

        private async Task LoadValues(HistoryConfigurationUsage usage)
        {
            try
            {
                _logger.LogInformation("Loading history configuration...");

                if (usage.UseServerAddress)
                {
                    await SetServerAddress();
                }

                _logger.LogInformation("Loading history configuration succeeded.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Loading history configuration failed.");
            }
        }

        private async Task HandleServerAddress(ChannelMessage channelMessage)
        {
            try
            {
                _logger.LogInformation("Server address change. {0} -> {1}.", channelMessage.Channel, channelMessage.Message);

                if (ChannelMessageIs(channelMessage, "set"))
                {
                    await SetServerAddress();
                }
                if (ChannelMessageIs(channelMessage, "del"))
                {
                    _logger.LogError("Key {0} was deleted.", HistoryKeys.ServerAddress);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error occurred while processing change from {0}.", channelMessage);
            }
        }

        private async Task SetServerAddress()
        {
            string serverAddress = await _repository.GetServerAddress();
            ServerAddress = serverAddress;
            _logger.LogInformation("Server address set to {1}.", serverAddress);
        }

        // TODO: consider reusing this
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
