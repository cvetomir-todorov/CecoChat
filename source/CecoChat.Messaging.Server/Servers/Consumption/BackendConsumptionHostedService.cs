using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CecoChat.Messaging.Server.Servers.Consumption
{
    public class BackendConsumptionHostedService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IBackendConsumer _backendConsumer;

        public BackendConsumptionHostedService(
            ILogger<BackendConsumptionHostedService> logger,
            IBackendConsumer backendConsumer)
        {
            _logger = logger;
            _backendConsumer = backendConsumer;
        }

        public Task StartAsync(CancellationToken ct)
        {
            _backendConsumer.Prepare();
            Task.Factory.StartNew(() => _backendConsumer.Start(CancellationToken.None), ct, TaskCreationOptions.LongRunning, TaskScheduler.Current);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
