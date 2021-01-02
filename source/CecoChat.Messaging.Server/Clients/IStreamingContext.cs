using System;
using System.Threading;
using System.Threading.Tasks;

namespace CecoChat.Messaging.Server.Clients
{
    public interface IStreamingContext<in TMessage> : IDisposable
    {
        void AddMessage(TMessage message);

        Task ProcessMessages(CancellationToken ct);
    }
}