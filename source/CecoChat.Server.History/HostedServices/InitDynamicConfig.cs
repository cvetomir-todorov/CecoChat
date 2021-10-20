using System.Threading;
using System.Threading.Tasks;
using CecoChat.Data.Config.History;
using Microsoft.Extensions.Hosting;

namespace CecoChat.Server.History.HostedServices
{
    public sealed class InitDynamicConfig : IHostedService
    {
        private readonly IHistoryConfig _historyConfig;

        public InitDynamicConfig(
            IHistoryConfig historyConfig)
        {
            _historyConfig = historyConfig;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _historyConfig.Initialize(new HistoryConfigUsage
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
