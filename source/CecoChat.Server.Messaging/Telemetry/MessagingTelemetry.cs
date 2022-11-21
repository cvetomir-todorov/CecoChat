using System.Diagnostics.Metrics;
using CecoChat.Server.Messaging.HostedServices;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.Messaging.Telemetry;

public interface IMessagingTelemetry : IDisposable
{
    void NotifyPlainTextReceived();

    void NotifyPlainTextProcessed();

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
        _plainTextsProcessed = _meter.CreateCounter<long>(MessagingInstrumentation.Metrics.MessagesProcessed, description: MessagingInstrumentation.Metrics.MessagesProcessedDescription);

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
