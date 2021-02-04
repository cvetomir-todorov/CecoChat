using System.Threading;
using System.Threading.Tasks;
using CecoChat.Data.Configuration.Messaging;
using Microsoft.Extensions.Hosting;

namespace CecoChat.Messaging.Server.Initialization
{
    public sealed class ConfigurationHostedService : IHostedService
    {
        private readonly IMessagingConfiguration _messagingConfiguration;

        public ConfigurationHostedService(
            IMessagingConfiguration messagingConfiguration)
        {
            _messagingConfiguration = messagingConfiguration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _messagingConfiguration.Initialize(new MessagingConfigurationUsage
            {
                UsePartitionCount = true
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
