using System.Threading;

namespace CecoChat.Materialize.Server.Backend
{
    public interface IBackendConsumer
    {
        void Prepare();
        void Start(CancellationToken ct);
    }
}