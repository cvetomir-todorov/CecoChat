using System;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Cassandra;
using CecoChat.Data.History;
using CecoChat.Data.History.Repos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CecoChat.Server.History.HostedServices
{
    public sealed class InitHistoryDb : IHostedService
    {
        private readonly ILogger _logger;
        private readonly ICassandraDbInitializer _dbInitializer;
        private readonly IHistoryRepo _historyRepo;
        private readonly INewMessageRepo _newMessageRepo;
        private readonly IReactionRepo _reactionRepo;

        public InitHistoryDb(
            ILogger<InitHistoryDb> logger,
            ICassandraDbInitializer dbInitializer,
            IHistoryRepo historyRepo,
            INewMessageRepo newMessageRepo,
            IReactionRepo reactionRepo)
        {
            _logger = logger;
            _dbInitializer = dbInitializer;
            _historyRepo = historyRepo;
            _newMessageRepo = newMessageRepo;
            _reactionRepo = reactionRepo;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _dbInitializer.Initialize(keyspace: "history", scriptSource: typeof(IHistoryDbContext).Assembly);

            Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Start preparing queries...");
                    _historyRepo.Prepare();
                    _newMessageRepo.Prepare();
                    _reactionRepo.Prepare();
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