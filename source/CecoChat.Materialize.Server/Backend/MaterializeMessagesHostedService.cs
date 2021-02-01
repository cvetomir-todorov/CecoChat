using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CecoChat.Materialize.Server.Backend
{
    public class MaterializeMessagesHostedService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IBackendConsumer _backendConsumer;

        public MaterializeMessagesHostedService(
            ILogger<MaterializeMessagesHostedService> logger,
            IBackendConsumer backendConsumer)
        {
            _logger = logger;
            _backendConsumer = backendConsumer;
        }

        public Task StartAsync(CancellationToken ct)
        {
            _backendConsumer.Prepare();
            Task.Factory.StartNew(() => _backendConsumer.Start(ct), ct, TaskCreationOptions.LongRunning, TaskScheduler.Current);

            _logger.LogInformation("Started materialize messages hosted service.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
