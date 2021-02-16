using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CecoChat.Data.Configuration
{
    public interface IConfigurationUtility
    {
        Task HandleChange(ChannelMessage channelMessage, Func<ChannelMessage, Task> handleAction);
    }

    public sealed class ConfigurationUtility : IConfigurationUtility
    {
        private readonly ILogger _logger;

        public ConfigurationUtility(
            ILogger<ConfigurationUtility> logger)
        {
            _logger = logger;
        }

        public async Task HandleChange(ChannelMessage channelMessage, Func<ChannelMessage, Task> handleAction)
        {
            try
            {
                _logger.LogInformation("Detected change {0}.", channelMessage);
                await handleAction(channelMessage);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error occurred while processing change {0}.", channelMessage);
            }
        }
    }
}
