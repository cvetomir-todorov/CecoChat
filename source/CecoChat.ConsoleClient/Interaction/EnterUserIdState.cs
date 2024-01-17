namespace CecoChat.ConsoleClient.Interaction;

public sealed class EnterUserIdState : State
{
    public EnterUserIdState(StateContainer states) : base(states)
    { }

    public override Task<State> Execute()
    {
        Console.Clear();
        DisplayUserData();
        DisplaySplitter();

        Console.Write("Enter user ID ('0' to exit): ");

        string? userIdString = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(userIdString) ||
            !long.TryParse(userIdString, out long userId) ||
            userId == 0)
        {
            Context.ReloadData = true;
            return Task.FromResult(States.AllChats);
        }
        else
        {
            Context.ReloadData = true;
            Context.UserId = userId;
            return Task.FromResult(States.OneChat);
        }
    }
}
