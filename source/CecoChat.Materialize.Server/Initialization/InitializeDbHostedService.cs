using System.Threading;
using System.Threading.Tasks;
using CecoChat.Data.Messaging;
using Microsoft.Extensions.Hosting;

namespace CecoChat.Materialize.Server.Initialization
{
    public sealed class InitializeDbHostedService : IHostedService
    {
        private readonly ICecoChatDbInitializer _dbInitializer;

        public InitializeDbHostedService(
            ICecoChatDbInitializer dbInitializer)
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
