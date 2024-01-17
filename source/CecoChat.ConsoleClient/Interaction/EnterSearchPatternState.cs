namespace CecoChat.ConsoleClient.Interaction;

public sealed class EnterSearchPatternState : State
{
    public EnterSearchPatternState(StateContainer states) : base(states)
    { }

    public override Task<State> Execute()
    {
        Console.Clear();
        DisplayUserData();
        DisplaySplitter();

        Console.WriteLine("Enter user search pattern which should be:");
        Console.WriteLine("1) letters including Unicode, no other symbols");
        Console.WriteLine("2) at least 3 symbols-long");
        Console.Write("Enter search pattern or press ENTER to exit: ");

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
