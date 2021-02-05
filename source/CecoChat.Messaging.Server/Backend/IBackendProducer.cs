using System;
using CecoChat.Contracts.Backend;

namespace CecoChat.Messaging.Server.Backend
{
    public interface IBackendProducer : IDisposable
    {
        int PartitionCount { get; set; }

        void ProduceMessage(BackendMessage message);
    }
}