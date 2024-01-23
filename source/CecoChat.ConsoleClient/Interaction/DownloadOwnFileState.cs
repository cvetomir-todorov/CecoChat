namespace CecoChat.ConsoleClient.Interaction;

public class DownloadOwnFileState : State
{
    public DownloadOwnFileState(StateContainer states) : base(states)
    { }

    public override async Task<State> Execute()
    {
        Console.Clear();
        DisplayUserData();
        DisplaySplitter();

        await DownloadFile(Context.DownloadFile.Bucket, Context.DownloadFile.Path);

        Context.ReloadData = true;
        return States.Files;
    }
}
