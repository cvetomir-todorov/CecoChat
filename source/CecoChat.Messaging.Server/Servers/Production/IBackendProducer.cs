using System;
using CecoChat.Contracts.Backend;

namespace CecoChat.Messaging.Server.Servers.Production
{
    public interface IBackendProducer : IDisposable
    {
        void ProduceMessage(Message message);

        // TODO: flush them
        void FlushPendingMessages();
    }
}