using System.Threading;
using System.Threading.Tasks;
using CecoChat.Data.Messaging;
using Microsoft.Extensions.Hosting;

namespace CecoChat.History.Server.Initialization
{
    public sealed class PrepareQueriesHostedService : IHostedService
    {
        private readonly IHistoryRepository _historyRepository;

        public PrepareQueriesHostedService(
            IHistoryRepository historyRepository)
        {
            _historyRepository = historyRepository;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _historyRepository.Prepare();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
