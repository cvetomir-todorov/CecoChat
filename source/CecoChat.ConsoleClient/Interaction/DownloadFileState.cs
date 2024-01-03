using CecoChat.ConsoleClient.Api;

namespace CecoChat.ConsoleClient.Interaction;

public class DownloadFileState : State
{
    public DownloadFileState(StateContainer states) : base(states)
    { }

    public override async Task<State> Execute()
    {
        Console.Clear();
        DisplayUserData();

        Console.Write("Enter the path to where the file downloaded should be saved or leave empty to exit: ");
        string? filePath = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Context.ReloadData = true;
            return States.Files;
        }

        if (File.Exists(filePath))
        {
            Console.WriteLine("File {0} already exists, press ENTER to exit", filePath);
            Console.ReadLine();

            Context.ReloadData = true;
            return States.Files;
        }

        Console.WriteLine("Downloading file {0}/{1}...", Context.DownloadFileBucket, Context.DownloadFilePath, filePath);
        ClientResponse<Stream> response = await Client.DownloadFile(Context.DownloadFileBucket, Context.DownloadFilePath);
        if (!response.Success)
        {
            DisplayErrors(response);

            Context.ReloadData = true;
            return States.Files;
        }

        await using FileStream localFile = new(filePath, FileMode.CreateNew, FileAccess.Write);
        await response.Content!.CopyToAsync(localFile);
        Console.WriteLine("Downloaded file successfully to {0}.", filePath);

        Console.Write("Press ENTER to continue...");
        Console.ReadLine();
        return States.Files;
    }
}
