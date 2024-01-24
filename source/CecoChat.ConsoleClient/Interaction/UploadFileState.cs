namespace CecoChat.ConsoleClient.Interaction;

public class UploadFileState : State
{
    public UploadFileState(StateContainer states) : base(states)
    { }

    public override async Task<State> Execute()
    {
        Console.Clear();
        DisplayUserData();
        DisplaySplitter();

        UploadFileResult result = await UploadFile(allowedUserId: 0);
        if (!result.Success)
        {
            Context.ReloadData = true;
            return States.Files;
        }

        Console.Write("Press ENTER to continue...");
        Console.ReadLine();

        Context.ReloadData = true;
        return States.Files;
    }
}
