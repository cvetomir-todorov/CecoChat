namespace CecoChat.ConsoleClient.Interaction;

public sealed class EnterSearchPatternState : State
{
    public EnterSearchPatternState(StateContainer states) : base(states)
    { }

    public override Task<State> Execute()
    {
        Console.Clear();
        DisplayUserData();
        Console.Write("Enter user search pattern (letters, no other symbols allowed) or press ENTER to exit: ");
        string? searchPattern = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(searchPattern))
        {
            Context.ReloadData = true;
            return Task.FromResult(States.AllChats);
        }
        else
        {
            Context.ReloadData = true;
            Context.SearchPattern = searchPattern;
            return Task.FromResult(States.SearchUsers);
        }
    }
}
