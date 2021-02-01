using System;
using System.Threading;

namespace CecoChat.Materialize.Server.Backend
{
    public interface IBackendConsumer : IDisposable
    {
        void Prepare();

        void Start(CancellationToken ct);
    }
}