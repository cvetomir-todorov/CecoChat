using CecoChat.Config.Backplane;
using CecoChat.Server;
using Common.AspNet.Init;

namespace CecoChat.IdGen.Service.Init;

public sealed class BackplaneComponentsInit : InitStep
{
    private readonly ILogger _logger;
    private readonly IConfigChangesConsumer _configChangesConsumer;
    private readonly ConfigChangesConsumerHealthCheck _configChangesConsumerHealthCheck;

    public BackplaneComponentsInit(
        ILogger<BackplaneComponentsInit> logger,
        IConfigChangesConsumer configChangesConsumer,
        ConfigChangesConsumerHealthCheck configChangesConsumerHealthCheck,
        IHostApplicationLifetime applicationLifetime)
        : base(applicationLifetime)
    {
        _logger = logger;
        _configChangesConsumer = configChangesConsumer;
        _configChangesConsumerHealthCheck = configChangesConsumerHealthCheck;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _configChangesConsumer.Dispose();
        }
    }

    protected override Task<bool> DoExecute(CancellationToken ct)
    {
        _configChangesConsumer.Prepare();

        Task.Factory.StartNew(() =>
        {
            try
            {
                _configChangesConsumerHealthCheck.IsReady = true;
                _configChangesConsumer.Start(ct);
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Failure in config changes consumer");
            }
            finally
            {
                _configChangesConsumerHealthCheck.IsReady = false;
            }
        }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Current);

        return Task.FromResult(true);
    }
}
