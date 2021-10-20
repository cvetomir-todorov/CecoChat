using System;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Materialize.Server.Backplane;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CecoChat.Materialize.Server.HostedServices
{
    public sealed class StartMaterializeMessages : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IMaterializeConsumer _materializeConsumer;
        private readonly CancellationToken _appStoppingCt;
        private CancellationTokenSource _stoppedCts;

        public StartMaterializeMessages(
            ILogger<StartMaterializeMessages> logger,
            IHostApplicationLifetime applicationLifetime,
            IMaterializeConsumer materializeConsumer)
        {
            _logger = logger;
            _materializeConsumer = materializeConsumer;

            _appStoppingCt = applicationLifetime.ApplicationStopping;
        }

        public void Dispose()
        {
            _stoppedCts?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _stoppedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _appStoppingCt);

            _materializeConsumer.Prepare();
            Task.Factory.StartNew(() =>
            {
                try
                {
                    _materializeConsumer.Start(_stoppedCts.Token);
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
