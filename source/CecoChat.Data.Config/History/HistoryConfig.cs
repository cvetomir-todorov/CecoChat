using System;
using System.Threading.Tasks;
using CecoChat.Redis;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CecoChat.Data.Config.History
{
    internal sealed class HistoryConfig : IHistoryConfig
    {
        private readonly ILogger _logger;
        private readonly IRedisContext _redisContext;
        private readonly IHistoryConfigRepo _repo;
        private readonly IConfigUtility _configUtility;

        private HistoryConfigUsage _usage;
        private HistoryConfigValues _values;
        private HistoryConfigValidator _validator;

        public HistoryConfig(
            ILogger<HistoryConfig> logger,
            IRedisContext redisContext,
            IHistoryConfigRepo repo,
            IConfigUtility configUtility)
        {
            _logger = logger;
            _redisContext = redisContext;
            _repo = repo;
            _configUtility = configUtility;
        }

        public int ChatMessageCount => _values.ChatMessageCount;

        public async Task Initialize(HistoryConfigUsage usage)
        {
            try
            {
                _usage = usage;
                await SubscribeForChanges(usage);

                _validator = new HistoryConfigValidator(usage);
                await LoadValidateValues(usage, _validator);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Initializing history configuration failed.");
            }
        }

        private async Task SubscribeForChanges(HistoryConfigUsage usage)
        {
            ISubscriber subscriber = _redisContext.GetSubscriber();

            if (usage.UseMessageCount)
            {
                ChannelMessageQueue chatMessageCountMQ = await subscriber.SubscribeAsync($"notify:{HistoryKeys.ChatMessageCount}");
                chatMessageCountMQ.OnMessage(channelMessage => _configUtility.HandleChange(channelMessage, HandleMessageCount));
                _logger.LogInformation("Subscribed for changes about {0} from channel {1}.",
                    HistoryKeys.ChatMessageCount, chatMessageCountMQ.Channel);
            }
        }

        private async Task HandleMessageCount(ChannelMessage channelMessage)
        {
            if (_usage.UseMessageCount)
            {
                await LoadValidateValues(_usage, _validator);
            }
        }

        private async Task LoadValidateValues(HistoryConfigUsage usage, HistoryConfigValidator validator)
        {
            HistoryConfigValues values = await _repo.GetValues(usage);
            _logger.LogInformation("Loading history configuration succeeded.");

            if (_configUtility.ValidateValues("history", values, validator))
            {
                _values = values;
                PrintValues(usage, values);
            }
        }

        private void PrintValues(HistoryConfigUsage usage, HistoryConfigValues values)
        {
            if (usage.UseMessageCount)
            {
                _logger.LogInformation("Chat message count set to {0}.", values.ChatMessageCount);
            }
        }
    }
}
