using CecoChat.Chats.Service.Backplane;
using CecoChat.DynamicConfig.Backplane;
using CecoChat.Server;
using Common.AspNet.Init;
using Common.Threading;

namespace CecoChat.Chats.Service.Init;

public sealed class BackplaneComponentsInit : InitStep
{
    private readonly ILogger _logger;
    private readonly IHistoryConsumer _historyConsumer;
    private readonly IStateConsumer _stateConsumer;
    private readonly IConfigChangesConsumer _configChangesConsumer;
    private readonly HistoryConsumerHealthCheck _historyConsumerHealthCheck;
    private readonly ReceiversConsumerHealthCheck _receiversConsumerHealthCheck;
    private readonly SendersConsumerHealthCheck _sendersConsumerHealthCheck;
    private readonly ConfigChangesConsumerHealthCheck _configChangesConsumerHealthCheck;
    private DedicatedThreadTaskScheduler? _historyConsumerTaskScheduler;
    private DedicatedThreadTaskScheduler? _receiverMessagesTaskScheduler;
    private DedicatedThreadTaskScheduler? _senderMessagesTaskScheduler;

    public BackplaneComponentsInit(
        ILogger<BackplaneComponentsInit> logger,
        IHistoryConsumer historyConsumer,
        IStateConsumer stateConsumer,
        IConfigChangesConsumer configChangesConsumer,
        HistoryConsumerHealthCheck historyConsumerHealthCheck,
        ReceiversConsumerHealthCheck receiversConsumerHealthCheck,
        SendersConsumerHealthCheck sendersConsumerHealthCheck,
        ConfigChangesConsumerHealthCheck configChangesConsumerHealthCheck,
        IHostApplicationLifetime applicationLifetime)
        : base(applicationLifetime)
    {
        _logger = logger;
        _historyConsumer = historyConsumer;
        _stateConsumer = stateConsumer;
        _configChangesConsumer = configChangesConsumer;
        _historyConsumerHealthCheck = historyConsumerHealthCheck;
        _receiversConsumerHealthCheck = receiversConsumerHealthCheck;
        _sendersConsumerHealthCheck = sendersConsumerHealthCheck;
        _configChangesConsumerHealthCheck = configChangesConsumerHealthCheck;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _historyConsumerTaskScheduler?.Dispose();
            _senderMessagesTaskScheduler?.Dispose();
            _receiverMessagesTaskScheduler?.Dispose();

            _historyConsumer.Dispose();
            _stateConsumer.Dispose();
            _configChangesConsumer.Dispose();
        }
    }

    protected override Task<bool> DoExecute(CancellationToken ct)
    {
        _historyConsumer.Prepare();
        _stateConsumer.Prepare();
        _configChangesConsumer.Prepare();

        StartHistoryConsumer(ct);
        StartReceiverMessagesConsumer(ct);
        StartSenderMessagesConsumer(ct);
        StartConfigChangesConsumer(ct);

        return Task.FromResult(true);
    }

    private void StartHistoryConsumer(CancellationToken ct)
    {
        _historyConsumerTaskScheduler = new DedicatedThreadTaskScheduler();
        Task.Factory.StartNew(() =>
        {
            try
            {
                _historyConsumerHealthCheck.IsReady = true;
                _historyConsumer.Start(ct);
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Failure in consumer {ConsumerId}", _historyConsumer.ConsumerId);
            }
            finally
            {
                _historyConsumerHealthCheck.IsReady = false;
            }
        }, ct, TaskCreationOptions.LongRunning, _historyConsumerTaskScheduler);
    }

    private void StartReceiverMessagesConsumer(CancellationToken ct)
    {
        _receiverMessagesTaskScheduler = new DedicatedThreadTaskScheduler();
        Task.Factory.StartNew(() =>
        {
            try
            {
                _receiversConsumerHealthCheck.IsReady = true;
                _stateConsumer.StartConsumingReceiverMessages(ct);
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Failure in consumer {ConsumerId}", _stateConsumer.ReceiverConsumerId);
            }
            finally
            {
                _receiversConsumerHealthCheck.IsReady = false;
            }
        }, ct, TaskCreationOptions.LongRunning, _receiverMessagesTaskScheduler);
    }

    private void StartSenderMessagesConsumer(CancellationToken ct)
    {
        _senderMessagesTaskScheduler = new DedicatedThreadTaskScheduler();
        Task.Factory.StartNew(() =>
        {
            try
            {
                _sendersConsumerHealthCheck.IsReady = true;
                _stateConsumer.StartConsumingSenderMessages(ct);
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Failure in consumer {ConsumerId}", _stateConsumer.SenderConsumerId);
            }
            finally
            {
                _sendersConsumerHealthCheck.IsReady = false;
            }
        }, ct, TaskCreationOptions.LongRunning, _senderMessagesTaskScheduler);
    }

    private void StartConfigChangesConsumer(CancellationToken ct)
    {
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
    }
}
