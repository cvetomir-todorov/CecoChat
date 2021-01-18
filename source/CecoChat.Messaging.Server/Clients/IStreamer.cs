using System;
using System.Threading;
using System.Threading.Tasks;

namespace CecoChat.Messaging.Server.Clients
{
    public interface IStreamer<in TMessage> : IDisposable
    {
        bool AddMessage(TMessage message);

        Task ProcessMessages(CancellationToken ct);
    }
}