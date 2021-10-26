using System.Threading;
using System.Threading.Tasks;
using CecoChat.Cassandra;
using CecoChat.Data.State;
using Microsoft.Extensions.Hosting;

namespace CecoChat.Server.State.HostedServices
{
    public sealed class InitStateDb : IHostedService
    {
        private readonly ICassandraDbInitializer _dbInitializer;

        public InitStateDb(
            ICassandraDbInitializer dbInitializer)
        {
            _dbInitializer = dbInitializer;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _dbInitializer.Initialize(keyspace: "state", scriptSource: typeof(IStateDbContext).Assembly);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}