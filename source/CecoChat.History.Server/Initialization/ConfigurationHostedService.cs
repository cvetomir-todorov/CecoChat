using System.Threading;
using System.Threading.Tasks;
using CecoChat.Data.Config.History;
using Microsoft.Extensions.Hosting;

namespace CecoChat.History.Server.Initialization
{
    public sealed class ConfigurationHostedService : IHostedService
    {
        private readonly IHistoryConfiguration _historyConfiguration;

        public ConfigurationHostedService(
            IHistoryConfiguration historyConfiguration)
        {
            _historyConfiguration = historyConfiguration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _historyConfiguration.Initialize(new HistoryConfigurationUsage
            {
                UseMessageCount = true
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
