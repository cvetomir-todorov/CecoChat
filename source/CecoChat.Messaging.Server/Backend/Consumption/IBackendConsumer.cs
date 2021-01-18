using System;
using System.Threading;

namespace CecoChat.Messaging.Server.Backend.Consumption
{
    public interface IBackendConsumer : IDisposable
    {
        void Prepare();

        void Start(CancellationToken ct);
    }
}