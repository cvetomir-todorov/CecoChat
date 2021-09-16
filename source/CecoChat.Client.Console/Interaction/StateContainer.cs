using System.Threading.Tasks;
using CecoChat.Client.Shared;

namespace CecoChat.Client.Console.Interaction
{
    public sealed class StateContainer
    {
        public StateContainer(MessagingClient client, MessageStorage storage)
        {
            Users = new UsersState(this, client, storage);
            Dialog = new DialogState(this, client, storage);
            SendMessage = new SendMessageState(this, client, storage);
            Final = new FinalState(this, client, storage);
        }

        public State Users { get; }
        public State Dialog { get; }
        public State SendMessage { get; }
        public State Final { get; }
    }
    
    public sealed class FinalState : State
    {
        public FinalState(StateContainer states, MessagingClient client, MessageStorage storage) : base(states, client, storage)
        {}

        public override Task<State> Execute(StateContext context)
        {
            return null;
        }
    }
}