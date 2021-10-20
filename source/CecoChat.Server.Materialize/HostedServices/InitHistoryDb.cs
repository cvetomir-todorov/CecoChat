using System.Threading;
using System.Threading.Tasks;
using CecoChat.Data.History;
using Microsoft.Extensions.Hosting;

namespace CecoChat.Materialize.Server.HostedServices
{
    public sealed class InitHistoryDb : IHostedService
    {
        private readonly IHistoryDbInitializer _dbInitializer;

        public InitHistoryDb(
            IHistoryDbInitializer dbInitializer)
        {
            _dbInitializer = dbInitializer;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _dbInitializer.Initialize();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
