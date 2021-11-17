using System.Threading.Tasks;
using CecoChat.ConsoleClient.Api;
using CecoChat.ConsoleClient.LocalStorage;

namespace CecoChat.ConsoleClient.Interaction
{
    public abstract class State
    {
        protected StateContainer States { get; }

        protected State(StateContainer states)
        {
            States = states;
        }

        protected MessageStorage Storage => States.Storage;
        protected MessagingClient Client => States.Client;
        protected StateContext Context => States.Context;

        public abstract Task<State> Execute();
    }
}