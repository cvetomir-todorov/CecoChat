using System;
using System.Threading;
using CecoChat.Kafka;

namespace CecoChat.Messaging.Server.Backend
{
    public interface IBackendConsumer : IDisposable
    {
        void Prepare(PartitionRange partitions);

        void Start(CancellationToken ct);

        string ConsumerID { get; }
    }
}