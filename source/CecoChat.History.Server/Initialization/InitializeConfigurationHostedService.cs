using System.Threading;
using System.Threading.Tasks;
using CecoChat.Data.Configuration.History;
using Microsoft.Extensions.Hosting;

namespace CecoChat.History.Server.Initialization
{
    public sealed class InitializeConfigurationHostedService : IHostedService
    {
        private readonly IHistoryConfiguration _historyConfiguration;

        public InitializeConfigurationHostedService(
            IHistoryConfiguration historyConfiguration)
        {
            _historyConfiguration = historyConfiguration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _historyConfiguration.Initialize(new HistoryConfigurationUsage
            {
                UseUserMessageCount = true,
                UseDialogMessageCount = true
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
