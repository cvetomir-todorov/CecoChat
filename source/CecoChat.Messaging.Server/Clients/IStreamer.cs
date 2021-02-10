using System;
using System.Threading;
using System.Threading.Tasks;

namespace CecoChat.Messaging.Server.Clients
{
    public interface IStreamer<TMessage> : IDisposable
    {
        Guid ClientID { get; }

        bool AddMessage(TMessage message);

        Task ProcessMessages(CancellationToken ct);

        void SetFinalMessagePredicate(Func<TMessage, bool> finalMessagePredicate);
    }
}