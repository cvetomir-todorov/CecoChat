using System.Threading;
using System.Threading.Tasks;
using CecoChat.Cassandra;
using CecoChat.Data.History;
using Microsoft.Extensions.Hosting;

namespace CecoChat.Server.History.HostedServices
{
    public sealed class InitHistoryDb : IHostedService
    {
        private readonly ICassandraDbInitializer _dbInitializer;

        public InitHistoryDb(
            ICassandraDbInitializer dbInitializer)
        {
            _dbInitializer = dbInitializer;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _dbInitializer.Initialize(keyspace: "history", scriptSource: typeof(IHistoryDbContext).Assembly);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}