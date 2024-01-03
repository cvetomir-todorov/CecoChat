using CecoChat.ConsoleClient.LocalStorage;

namespace CecoChat.ConsoleClient.Interaction;

public class FilesState : State
{
    public FilesState(StateContainer states) : base(states)
    { }

    public override Task<State> Execute()
    {
        if (Context.ReloadData)
        {
            // TODO: load files
        }

        Console.Clear();
        DisplayUserData();
        Console.WriteLine("Download a file (press '0'...'9') | Upload a file (press 'u')");
        Console.WriteLine("Exit (press 'x')");
        DisplaySplitter();
        List<FileRef> userFiles = DisplayUserFiles();

        ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
        if (char.IsNumber(keyInfo.KeyChar))
        {
            State state = ProcessNumberKey(keyInfo, userFiles);
            return Task.FromResult(state);
        }
        else if (keyInfo.KeyChar == 'u')
        {
            Context.ReloadData = true;
            return Task.FromResult(States.UploadFile);
        }
        else if (keyInfo.KeyChar == 'x')
        {
            Context.ReloadData = true;
            return Task.FromResult(States.AllChats);
        }
        else
        {
            // includes local refresh
            Context.ReloadData = false;
            return Task.FromResult(States.Files);
        }
    }

    private new List<FileRef> DisplayUserFiles()
    {
        List<FileRef> userFiles = new();
        int key = 0;

        foreach (FileRef userFile in UserFiles.EnumerateUserFiles())
        {
            DisplayUserFile(userFile, key);
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
            Context.DownloadFileBucket = userFiles[index].Bucket;
            Context.DownloadFilePath = userFiles[index].Path;

            Context.ReloadData = true;
            return States.DownloadFile;
        }
    } 
}
