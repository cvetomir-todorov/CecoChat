using System.Threading;
using System.Threading.Tasks;
using CecoChat.Data.Configuration.Messaging;
using CecoChat.Messaging.Server.Backend;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Initialization
{
    public sealed class ConfigurationHostedService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IBackendOptions _backendOptions;
        private readonly IMessagingConfiguration _messagingConfiguration;

        public ConfigurationHostedService(
            ILogger<ConfigurationHostedService> logger,
            IOptions<BackendOptions> backendOptions,
            IMessagingConfiguration messagingConfiguration)
        {
            _logger = logger;
            _backendOptions = backendOptions.Value;
            _messagingConfiguration = messagingConfiguration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Configured server ID is '{0}'.", _backendOptions.ServerID);

            await _messagingConfiguration.Initialize(new MessagingConfigurationUsage
            {
                UsePartitions = true,
                ServerForWhichToUsePartitions = _backendOptions.ServerID
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
