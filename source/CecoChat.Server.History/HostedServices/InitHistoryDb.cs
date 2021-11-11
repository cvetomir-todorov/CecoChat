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
        private readonly IChatMessageRepo _messageRepo;

        public InitHistoryDb(
            ILogger<InitHistoryDb> logger,
            ICassandraDbInitializer dbInitializer,
            IChatMessageRepo messageRepo)
        {
            _logger = logger;
            _dbInitializer = dbInitializer;
            _messageRepo = messageRepo;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _dbInitializer.Initialize(keyspace: "history", scriptSource: typeof(IHistoryDbContext).Assembly);

            Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Start preparing queries...");
                    _messageRepo.Prepare();
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