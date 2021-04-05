using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CecoChat.Messaging.Server.Clients
{
    public interface IStreamer<in TMessage> : IDisposable
    {
        Guid ClientID { get; }

        bool EnqueueMessage(TMessage message, Activity parentActivity = null);

        Task ProcessMessages(CancellationToken ct);
    }
}