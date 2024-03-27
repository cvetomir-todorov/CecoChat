using System.Diagnostics.Metrics;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Service.Telemetry;

public interface IMessagingTelemetry : IDisposable
{
    void NotifyPlainTextReceived();

    void NotifyPlainTextProcessed();

    void NotifyFileReceived();

    void NotifyFileProcessed();

    void NotifyReactionReceived();

    void NotifyReactionProcessed();

    void NotifyUnreactionReceived();

    void NotifyUnreactionProcessed();

    void AddOnlineClient();

    void RemoveOnlineClient();
}

public sealed class MessagingTelemetry : IMessagingTelemetry
{
    private readonly Meter _meter;
    private readonly Counter<long> _plainTextsReceived;
    private readonly Counter<long> _plainTextsProcessed;
    private readonly Counter<long> _filesReceived;
    private readonly Counter<long> _filesProcessed;
    private readonly Counter<long> _reactionsReceived;
    private readonly Counter<long> _reactionsProcessed;
    private readonly Counter<long> _unreactionsReceived;
    private readonly Counter<long> _unreactionsProcessed;
    private readonly UpDownCounter<int> _onlineClients;
    private readonly KeyValuePair<string, object?> _serverIdTag;

    public MessagingTelemetry(IOptions<ConfigOptions> configOptions)
    {
        _meter = new Meter(MessagingInstrumentation.ActivitySource.Name);

        _plainTextsReceived = _meter.CreateCounter<long>(MessagingInstrumentation.Metrics.PlainTextsReceived, description: MessagingInstrumentation.Metrics.PlainTextsReceivedDescription);
        _plainTextsProcessed = _meter.CreateCounter<long>(MessagingInstrumentation.Metrics.PlainTextsProcessed, description: MessagingInstrumentation.Metrics.PlainTextsProcessedDescription);

        _filesReceived = _meter.CreateCounter<long>(MessagingInstrumentation.Metrics.FilesReceived, description: MessagingInstrumentation.Metrics.FilesReceivedDescription);
        _filesProcessed = _meter.CreateCounter<long>(MessagingInstrumentation.Metrics.FilesProcessed, description: MessagingInstrumentation.Metrics.FilesProcessedDescription);

        _reactionsReceived = _meter.CreateCounter<long>(MessagingInstrumentation.Metrics.ReactionsReceived, description: MessagingInstrumentation.Metrics.ReactionsReceivedDescription);
        _reactionsProcessed = _meter.CreateCounter<long>(MessagingInstrumentation.Metrics.ReactionsProcessed, description: MessagingInstrumentation.Metrics.ReactionsProcessedDescription);

        _unreactionsReceived = _meter.CreateCounter<long>(MessagingInstrumentation.Metrics.UnReactionsReceived, description: MessagingInstrumentation.Metrics.UnReactionsReceivedDescription);
        _unreactionsProcessed = _meter.CreateCounter<long>(MessagingInstrumentation.Metrics.UnReactionsProcessed, description: MessagingInstrumentation.Metrics.UnReactionsProcessedDescription);

        _onlineClients = _meter.CreateUpDownCounter<int>(MessagingInstrumentation.Metrics.OnlineClients, description: MessagingInstrumentation.Metrics.OnlineClientsDescription);

        _serverIdTag = new KeyValuePair<string, object?>(MessagingInstrumentation.Tags.ServerId, configOptions.Value.ServerId);
    }

    public void Dispose()
    {
        _meter.Dispose();
    }

    public void NotifyPlainTextReceived()
    {
        _plainTextsReceived.Add(1, _serverIdTag);
    }

    public void NotifyPlainTextProcessed()
    {
        _plainTextsProcessed.Add(1, _serverIdTag);
    }

    public void NotifyFileReceived()
    {
        _filesReceived.Add(1, _serverIdTag);
    }

    public void NotifyFileProcessed()
    {
        _filesProcessed.Add(1, _serverIdTag);
    }

    public void NotifyReactionReceived()
    {
        _reactionsReceived.Add(1, _serverIdTag);
    }

    public void NotifyReactionProcessed()
    {
        _reactionsProcessed.Add(1, _serverIdTag);
    }

    public void NotifyUnreactionReceived()
    {
        _unreactionsReceived.Add(1, _serverIdTag);
    }

    public void NotifyUnreactionProcessed()
    {
        _unreactionsProcessed.Add(1, _serverIdTag);
    }

    public void AddOnlineClient()
    {
        _onlineClients.Add(1, _serverIdTag);
    }

    public void RemoveOnlineClient()
    {
        _onlineClients.Add(-1, _serverIdTag);
    }
}
