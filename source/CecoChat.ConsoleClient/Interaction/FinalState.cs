namespace CecoChat.ConsoleClient.Interaction;

public sealed class FinalState : State
{
    public FinalState(StateContainer states) : base(states)
    { }

    public override Task<State> Execute()
    {
        throw new NotSupportedException();
    }
}
