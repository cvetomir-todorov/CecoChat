using System.Diagnostics;

namespace CecoChat.Server.Messaging.Clients.Streaming;

public interface IStreamer<in TMessage> : IDisposable
{
    Guid ClientID { get; }

    bool EnqueueMessage(TMessage message, Activity? parentActivity = null);

    Task ProcessMessages(CancellationToken ct);
}