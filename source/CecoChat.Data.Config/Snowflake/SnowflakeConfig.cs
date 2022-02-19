using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CecoChat.Redis;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CecoChat.Data.Config.Snowflake
{
    internal sealed class SnowflakeConfig : ISnowflakeConfig
    {
        private readonly ILogger _logger;
        private readonly IRedisContext _redisContext;
        private readonly ISnowflakeConfigRepo _repo;
        private readonly IConfigUtility _configUtility;

        private SnowflakeConfigValues _values;
        private SnowflakeConfigValidator _validator; // TODO: make validator readonly field

        public SnowflakeConfig(
            ILogger<SnowflakeConfig> logger,
            IRedisContext redisContext,
            ISnowflakeConfigRepo repo,
            IConfigUtility configUtility)
        {
            _logger = logger;
            _redisContext = redisContext;
            _repo = repo;
            _configUtility = configUtility;
        }

        public void Dispose()
        {
            _redisContext.Dispose();
        }

        public IReadOnlyCollection<short> GetGeneratorIDs(string server)
        {
            if (!_values.ServerGeneratorIDs.TryGetValue(server, out List<short> generatorIDs))
            {
                throw new InvalidOperationException($"No snowflake generator IDs configured for server {server}.");
            }

            return generatorIDs;
        }

        public async Task Initialize()
        {
            try
            {
                await SubscribeForChanges();

                _validator = new SnowflakeConfigValidator();
                await LoadValidateValues(_validator);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Initializing snowflake config failed.");
            }
        }

        private async Task SubscribeForChanges()
        {
            ISubscriber subscriber = _redisContext.GetSubscriber();

            ChannelMessageQueue generatorIDsMQ = await subscriber.SubscribeAsync($"notify:{SnowflakeKeys.ServerGeneratorIDs}");
            generatorIDsMQ.OnMessage(channelMessage => _configUtility.HandleChange(channelMessage, HandleGeneratorIDs));
            _logger.LogInformation("Subscribed for changes about {0} from channel {1}.",
                SnowflakeKeys.ServerGeneratorIDs, generatorIDsMQ.Channel);
        }

        private Task HandleGeneratorIDs(ChannelMessage channelMessage)
        {
            return LoadValidateValues(_validator);
        }

        private async Task LoadValidateValues(SnowflakeConfigValidator validator)
        {
            SnowflakeConfigValues values = await _repo.GetValues();
            _logger.LogInformation("Loading snowflake configuration succeeded.");

            bool areValid = _configUtility.ValidateValues("snowflake", values, validator);
            if (areValid)
            {
                _values = values;
                PrintValues(values);
            }
        }

        private void PrintValues(SnowflakeConfigValues values)
        {
            foreach (KeyValuePair<string, List<short>> pair in values.ServerGeneratorIDs)
            {
                _logger.LogInformation("Server {0} is assigned generator IDs: [{1}].", pair.Key, string.Join(separator: ", ", pair.Value));
            }
        }
    }
}