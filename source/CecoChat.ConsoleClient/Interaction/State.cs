using CecoChat.Client.Messaging;
using CecoChat.ConsoleClient.Api;
using CecoChat.ConsoleClient.LocalStorage;
using CecoChat.Data;

namespace CecoChat.ConsoleClient.Interaction;

public abstract class State
{
    protected StateContainer States { get; }

    protected State(StateContainer states)
    {
        States = states;
        FileUtility = new FileUtility();
    }

    protected MessageStorage MessageStorage => States.MessageStorage;
    protected ConnectionStorage ConnectionStorage => States.ConnectionStorage;
    protected ProfileStorage ProfileStorage => States.ProfileStorage;
    protected FileStorage UserFiles => States.FileStorage;
    protected ChatClient Client => States.Client;
    protected IMessagingClient MessagingClient => States.MessagingClient;
    protected StateContext Context => States.Context;
    protected IFileUtility FileUtility { get; private set; }

    public abstract Task<State> Execute();

    protected static void DisplaySplitter()
    {
        Console.WriteLine("=================================================================================================");
    }

    protected void DisplayUserData()
    {
        if (Client.UserProfile == null)
        {
            throw new InvalidOperationException("Client has not connected.");
        }

        Console.WriteLine("You: {0} | ID={1} | user name={2} | email={3} | phone={4} | avatar={5}",
            Client.UserProfile.DisplayName, Client.UserId, Client.UserProfile.UserName,
            Client.UserProfile.Email, Client.UserProfile.Phone, Client.UserProfile.AvatarUrl);
    }

    protected static void DisplayErrors(ClientResponse response)
    {
        if (response.Success)
        {
            throw new ArgumentException("Response should not be successful when errors are being displayed.", paramName: nameof(response));
        }

        foreach (string error in response.Errors)
        {
            Console.WriteLine(error);
        }

        Console.WriteLine("Press ENTER to return.");
        Console.ReadLine();
    }

    protected List<FileRef> DisplayUserFiles()
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

    protected readonly struct UploadFileResult
    {
        public bool Success { get; init; }
        public string Bucket { get; init; }
        public string Path { get; init; }
    }

    protected async Task<UploadFileResult> UploadFile(long allowedUserId)
    {
        Console.Write("Enter the path to the file to be uploaded or leave empty to exit: ");
        string? filePath = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return new UploadFileResult
            {
                Success = false
            };
        }

        if (!File.Exists(filePath))
        {
            Console.WriteLine("File {0} doesn't exist, press ENTER to exit", filePath);
            Console.ReadLine();

            return new UploadFileResult
            {
                Success = false
            };
        }

        string extension = Path.GetExtension(filePath);
        string contentType = FileUtility.GetContentType(extension);

        Console.WriteLine("Sending file {0} with content type {1}...", filePath, contentType);
        await using Stream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        string fileName = Path.GetFileName(filePath);
        ClientResponse<Bff.Contracts.Files.UploadFileResponse> response = await Client.UploadFile(fileStream, fileName, contentType, allowedUserId);

        if (!response.Success)
        {
            Console.WriteLine("Uploading file resulted in {0} errors:", response.Errors.Count);
            DisplayErrors(response);

            return new UploadFileResult
            {
                Success = false
            };
        }

        Console.WriteLine("Uploaded file {0}/{1} successfully.", response.Content!.File.Bucket, response.Content!.File.Path);

        return new UploadFileResult
        {
            Success = true,
            Bucket = response.Content.File.Bucket,
            Path = response.Content.File.Path
        };
    }

    protected async Task<bool> DownloadFile(string bucket, string path)
    {
        Console.Write("Enter the path to where the file downloaded should be saved or leave empty to exit: ");
        string? filePath = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        if (File.Exists(filePath))
        {
            Console.WriteLine("File {0} already exists, press ENTER to exit", filePath);
            Console.ReadLine();

            return false;
        }

        Console.WriteLine("Downloading file {0}/{1}...", bucket, path);
        ClientResponse<Stream> response = await Client.DownloadFile(bucket, path);
        if (!response.Success)
        {
            Console.WriteLine("Downloading file result in {0} errors:", response.Errors.Count);
            DisplayErrors(response);

            return false;
        }

        await using FileStream localFile = new(filePath, FileMode.CreateNew, FileAccess.Write);
        await response.Content!.CopyToAsync(localFile);
        Console.WriteLine("Downloaded file successfully to {0}.", filePath);
        Console.Write("Press ENTER to continue...");
        Console.ReadLine();

        return true;
    }
}
