using System;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Materialize.Server.Backend;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CecoChat.Materialize.Server.Initialization
{
    public sealed class MaterializeMessagesHostedService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IMaterializeMessagesConsumer _materializeMessagesConsumer;
        private readonly CancellationToken _appStoppingCt;
        private CancellationTokenSource _stoppedCts;

        public MaterializeMessagesHostedService(
            ILogger<MaterializeMessagesHostedService> logger,
            IHostApplicationLifetime applicationLifetime,
            IMaterializeMessagesConsumer materializeMessagesConsumer)
        {
            _logger = logger;
            _materializeMessagesConsumer = materializeMessagesConsumer;

            _appStoppingCt = applicationLifetime.ApplicationStopping;
        }

        public void Dispose()
        {
            _stoppedCts?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _stoppedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _appStoppingCt);

            _materializeMessagesConsumer.Prepare();
            Task.Factory.StartNew(() =>
            {
                try
                {
                    _materializeMessagesConsumer.Start(_stoppedCts.Token);
                }
                catch (Exception exception)
                {
                    _logger.LogCritical(exception, "Failure in materialize messages consumer.");
                }
            }, _stoppedCts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
