using System.Threading;
using System.Threading.Tasks;
using CecoChat.Data.Configuration.History;
using CecoChat.Data.Configuration.Messaging;
using Microsoft.Extensions.Hosting;

namespace CecoChat.Connect.Server.Initialization
{
    public sealed class InitializeConfigurationHostedService : IHostedService
    {
        private readonly IMessagingConfiguration _messagingConfiguration;
        private readonly IHistoryConfiguration _historyConfiguration;

        public InitializeConfigurationHostedService(
            IMessagingConfiguration messagingConfiguration,
            IHistoryConfiguration historyConfiguration)
        {
            _messagingConfiguration = messagingConfiguration;
            _historyConfiguration = historyConfiguration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _messagingConfiguration.Initialize(new MessagingConfigurationUsage
            {
                UsePartitionCount = true,
                UseServerAddressByPartition = true
            });
            await _historyConfiguration.Initialize(new HistoryConfigurationUsage
            {
                UseServerAddress = true
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
