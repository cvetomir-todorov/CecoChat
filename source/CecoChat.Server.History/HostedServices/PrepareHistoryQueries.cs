using System;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Data.History;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CecoChat.Server.History.HostedServices
{
    public sealed class PrepareHistoryQueries : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IHistoryRepository _historyRepository;
        private readonly INewMessageRepository _newMessageRepository;
        private readonly IReactionRepository _reactionRepository;

        public PrepareHistoryQueries(
            ILogger<PrepareHistoryQueries> logger,
            IHistoryRepository historyRepository,
            INewMessageRepository newMessageRepository,
            IReactionRepository reactionRepository)
        {
            _logger = logger;
            _historyRepository = historyRepository;
            _newMessageRepository = newMessageRepository;
            _reactionRepository = reactionRepository;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Start preparing queries...");
                    _historyRepository.Prepare();
                    _newMessageRepository.Prepare();
                    _reactionRepository.Prepare();
                    _logger.LogInformation("Completed preparing queries.");
                }
                catch (Exception exception)
                {
                    _logger.LogCritical(exception, "Failed to prepare queries.");
                }
            }, cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
