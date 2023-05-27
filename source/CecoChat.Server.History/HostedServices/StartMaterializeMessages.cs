using CecoChat.Server.History.Backplane;
using CecoChat.Threading;

namespace CecoChat.Server.History.HostedServices;

public sealed class StartMaterializeMessages : IHostedService, IDisposable
{
    private readonly ILogger _logger;
    private readonly IHistoryConsumer _historyConsumer;
    private readonly HistoryConsumerHealthCheck _historyConsumerHealthCheck;
    private readonly CancellationToken _appStoppingCt;
    private CancellationTokenSource? _stoppedCts;
    private DedicatedThreadTaskScheduler? _historyConsumerTaskScheduler;

    public StartMaterializeMessages(
        ILogger<StartMaterializeMessages> logger,
        IHistoryConsumer historyConsumer,
        HistoryConsumerHealthCheck historyConsumerHealthCheck,
        IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger;
        _historyConsumer = historyConsumer;
        _historyConsumerHealthCheck = historyConsumerHealthCheck;

        _appStoppingCt = applicationLifetime.ApplicationStopping;
    }

    public void Dispose()
    {
        _historyConsumerTaskScheduler?.Dispose();
        _stoppedCts?.Dispose();
        _historyConsumer.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _appStoppingCt);

        _historyConsumer.Prepare();
        _historyConsumerTaskScheduler = new DedicatedThreadTaskScheduler();

        Task.Factory.StartNew(() =>
        {
            try
            {
                _historyConsumerHealthCheck.IsReady = true;
                _historyConsumer.Start(_stoppedCts.Token);
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Failure in consumer {ConsumerId}", _historyConsumer.ConsumerId);
            }
            finally
            {
                _historyConsumerHealthCheck.IsReady = false;
            }
        }, _stoppedCts.Token, TaskCreationOptions.LongRunning, _historyConsumerTaskScheduler);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
