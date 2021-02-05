using System;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Materialize.Server.Backend;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CecoChat.Materialize.Server.Initialization
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
            Task.Factory.StartNew(() =>
            {
                try
                {
                    _backendConsumer.Start(ct);
                }
                catch (Exception exception)
                {
                    _logger.LogCritical(exception, "Failure in start materialize messages hosted service.");
                }
            }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Current);

            _logger.LogInformation("Started materialize messages hosted service.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
