using System.Threading.Tasks;

namespace CecoChat.Client.Console.Interaction
{
    public sealed class FinalState : State
    {
        public FinalState(StateContainer states) : base(states)
        {}

        public override Task<State> Execute()
        {
            return Task.FromResult<State>(null);
        }
    }
}