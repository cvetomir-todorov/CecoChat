using System;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Server.State.Backplane;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CecoChat.Server.State.HostedServices
{
    public sealed class StartBackplaneComponents : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IStateConsumer _stateConsumer;
        private readonly CancellationToken _appStoppingCt;
        private CancellationTokenSource _stoppedCts;

        public StartBackplaneComponents(
            ILogger<StartBackplaneComponents> logger,
            IStateConsumer stateConsumer,
            IHostApplicationLifetime applicationLifetime)
        {
            _logger = logger;
            _stateConsumer = stateConsumer;

            _appStoppingCt = applicationLifetime.ApplicationStopping;
        }

        public void Dispose()
        {
            _stoppedCts?.Dispose();
            _stateConsumer.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _stoppedCts = CancellationTokenSource.CreateLinkedTokenSource(_appStoppingCt);

            _stateConsumer.Prepare();

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _stateConsumer.Start(_stoppedCts.Token);
                }
                catch (Exception exception)
                {
                    _logger.LogCritical(exception, "Failure in state consumer.");
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