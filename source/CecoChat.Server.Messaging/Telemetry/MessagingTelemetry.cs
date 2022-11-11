using System.Diagnostics.Metrics;
using CecoChat.Server.Messaging.HostedServices;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.Messaging.Telemetry;

public interface IMessagingTelemetry : IDisposable
{
    void NotifyMessageReceived();

    void NotifyMessageProcessed();

    void AddOnlineClient();

    void RemoveOnlineClient();
}

public sealed class MessagingTelemetry : IMessagingTelemetry
{
    private readonly Meter _meter;
    private readonly Counter<long> _messagesReceived;
    private readonly Counter<long> _messagesProcessed;
    private readonly UpDownCounter<int> _onlineClients;
    private readonly KeyValuePair<string, object?> _serverIdTag;

    public MessagingTelemetry(IOptions<ConfigOptions> configOptions)
    {
        _meter = new Meter(MessagingInstrumentation.ActivitySource.Name);

        _messagesReceived = _meter.CreateCounter<long>(MessagingInstrumentation.Metrics.MessagesReceived, description: MessagingInstrumentation.Metrics.MessagesReceivedDescription);
        _messagesProcessed = _meter.CreateCounter<long>(MessagingInstrumentation.Metrics.MessagesProcessed, description: MessagingInstrumentation.Metrics.MessagesProcessedDescription);
        _onlineClients = _meter.CreateUpDownCounter<int>(MessagingInstrumentation.Metrics.OnlineClients, description: MessagingInstrumentation.Metrics.OnlineClientsDescription);

        _serverIdTag = new KeyValuePair<string, object?>(MessagingInstrumentation.Tags.ServerId, configOptions.Value.ServerID);
    }

    public void Dispose()
    {
        _meter.Dispose();
    }

    public void NotifyMessageReceived()
    {
        _messagesReceived.Add(1, _serverIdTag);
    }

    public void NotifyMessageProcessed()
    {
        _messagesProcessed.Add(1, _serverIdTag);
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
