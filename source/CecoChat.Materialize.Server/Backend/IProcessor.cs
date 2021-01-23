using CecoChat.Contracts.Backend;

namespace CecoChat.Materialize.Server.Backend
{
    public interface IProcessor
    {
        void Process(Message message);
    }
}
