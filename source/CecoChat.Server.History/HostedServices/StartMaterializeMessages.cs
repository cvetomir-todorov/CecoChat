using CecoChat.Server.History.Backplane;

namespace CecoChat.Server.History.HostedServices;

public sealed class StartMaterializeMessages : IHostedService, IDisposable
{
    private readonly ILogger _logger;
    private readonly IHistoryConsumer _historyConsumer;
    private readonly CancellationToken _appStoppingCt;
    private CancellationTokenSource? _stoppedCts;

    public StartMaterializeMessages(
        ILogger<StartMaterializeMessages> logger,
        IHostApplicationLifetime applicationLifetime,
        IHistoryConsumer historyConsumer)
    {
        _logger = logger;
        _historyConsumer = historyConsumer;

        _appStoppingCt = applicationLifetime.ApplicationStopping;
    }

    public void Dispose()
    {
        _stoppedCts?.Dispose();
        _historyConsumer.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _appStoppingCt);

        _historyConsumer.Prepare();

        Task.Factory.StartNew(() =>
        {
            try
            {
                _historyConsumer.Start(_stoppedCts.Token);
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Failure in consumer {0}.", _historyConsumer.ConsumerID);
            }
        }, _stoppedCts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}