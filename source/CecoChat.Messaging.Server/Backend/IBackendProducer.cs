using System;
using CecoChat.Contracts.Backend;

namespace CecoChat.Messaging.Server.Backend
{
    public interface IBackendProducer : IDisposable
    {
        void ProduceMessage(BackendMessage message);
    }
}