using CecoChat.DynamicConfig.Backplane;

namespace CecoChat.Server.IdGen.HostedServices;

public sealed class InitBackplaneComponents : IHostedService, IDisposable
{
    private readonly ILogger _logger;
    private readonly IConfigChangesConsumer _configChangesConsumer;
    private readonly ConfigChangesConsumerHealthCheck _configChangesConsumerHealthCheck;
    private readonly CancellationToken _appStoppingCt;
    private CancellationTokenSource? _stoppedCts;

    public InitBackplaneComponents(
        ILogger<InitBackplaneComponents> logger,
        IConfigChangesConsumer configChangesConsumer,
        ConfigChangesConsumerHealthCheck configChangesConsumerHealthCheck,
        IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger;
        _configChangesConsumer = configChangesConsumer;
        _configChangesConsumerHealthCheck = configChangesConsumerHealthCheck;

        _appStoppingCt = applicationLifetime.ApplicationStopping;
    }

    public void Dispose()
    {
        _stoppedCts?.Dispose();
        _configChangesConsumer.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppedCts = CancellationTokenSource.CreateLinkedTokenSource(_appStoppingCt, cancellationToken);

        _configChangesConsumer.Prepare();

        Task.Factory.StartNew(() =>
        {
            try
            {
                _configChangesConsumerHealthCheck.IsReady = true;
                _configChangesConsumer.Start(_stoppedCts.Token);
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Failure in config changes consumer");
            }
            finally
            {
                _configChangesConsumerHealthCheck.IsReady = false;
            }
        }, _stoppedCts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
