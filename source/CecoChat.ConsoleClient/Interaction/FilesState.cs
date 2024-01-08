using CecoChat.ConsoleClient.LocalStorage;

namespace CecoChat.ConsoleClient.Interaction;

public class FilesState : State
{
    public FilesState(StateContainer states) : base(states)
    { }

    public override async Task<State> Execute()
    {
        if (Context.ReloadData)
        {
            await Load();
        }

        Console.Clear();
        DisplayUserData();
        Console.WriteLine("Download a file (press '0'...'9') | Upload a file (press 'u') | Refresh (press 'f')");
        Console.WriteLine("Exit (press 'x')");
        DisplaySplitter();
        List<FileRef> userFiles = DisplayUserFiles();

        ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
        if (char.IsNumber(keyInfo.KeyChar))
        {
            State state = ProcessNumberKey(keyInfo, userFiles);
            return state;
        }
        else if (keyInfo.KeyChar == 'u')
        {
            Context.ReloadData = true;
            return States.UploadFile;
        }
        else if (keyInfo.KeyChar == 'f')
        {
            Context.ReloadData = true;
            return States.Files;
        }
        else if (keyInfo.KeyChar == 'x')
        {
            Context.ReloadData = true;
            return States.AllChats;
        }
        else
        {
            // includes local refresh
            Context.ReloadData = false;
            return States.Files;
        }
    }

    private async Task Load()
    {
        DateTime newLastKnownState = DateTime.UtcNow;

        List<FileRef> userFiles = await Client.GetUserFiles(newerThan: Context.LastKnownFilesState);
        UserFiles.UpdateUserFiles(userFiles);

        Context.LastKnownFilesState = newLastKnownState;
    }

    private List<FileRef> DisplayUserFiles()
    {
        List<FileRef> userFiles = new();
        int key = 0;

        foreach (FileRef userFile in UserFiles.EnumerateUserFiles().OrderByDescending(file => file.Version))
        {
            Console.WriteLine("Press '{0}' for: {1}/{2}    {3:F}", key, userFile.Bucket, userFile.Path, userFile.Version);
            userFiles.Add(userFile);
            key++;
        }

        return userFiles;
    }

    private State ProcessNumberKey(ConsoleKeyInfo keyInfo, List<FileRef> userFiles)
    {
        int index = keyInfo.KeyChar - '0';
        if (index < 0 || index >= userFiles.Count)
        {
            Context.ReloadData = false;
            return States.Files;
        }
        else
        {
            Context.DownloadFile = userFiles[index];
            Context.ReloadData = true;
            return States.DownloadFile;
        }
    }
}
