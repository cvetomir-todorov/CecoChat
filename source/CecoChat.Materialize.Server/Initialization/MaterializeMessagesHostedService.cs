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
        private readonly IMaterializeMessagesConsumer _materializeMessagesConsumer;

        public MaterializeMessagesHostedService(
            ILogger<MaterializeMessagesHostedService> logger,
            IMaterializeMessagesConsumer materializeMessagesConsumer)
        {
            _logger = logger;
            _materializeMessagesConsumer = materializeMessagesConsumer;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _materializeMessagesConsumer.Prepare();
            Task.Factory.StartNew(() =>
            {
                try
                {
                    _materializeMessagesConsumer.Start(cancellationToken);
                }
                catch (Exception exception)
                {
                    _logger.LogCritical(exception, "Failure in materialize messages consumer.");
                }
            }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
