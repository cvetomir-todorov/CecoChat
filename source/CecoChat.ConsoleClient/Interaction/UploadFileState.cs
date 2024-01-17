using CecoChat.ConsoleClient.Api;
using CecoChat.Data;

namespace CecoChat.ConsoleClient.Interaction;

public class UploadFileState : State
{
    private readonly IFileUtility _fileUtility;

    public UploadFileState(StateContainer states) : base(states)
    {
        _fileUtility = new FileUtility();
    }

    public override async Task<State> Execute()
    {
        Console.Clear();
        DisplayUserData();
        DisplaySplitter();

        Console.Write("Enter the path to the file to be uploaded or leave empty to exit: ");
        string? filePath = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Context.ReloadData = true;
            return States.Files;
        }

        if (!File.Exists(filePath))
        {
            Console.WriteLine("File {0} doesn't exist, press ENTER to exit", filePath);
            Console.ReadLine();

            Context.ReloadData = true;
            return States.Files;
        }

        string contentType = GetContentType(filePath);

        Console.WriteLine("Sending file {0} with content type {1}...", filePath, contentType);
        await using Stream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        string fileName = Path.GetFileName(filePath);
        ClientResponse<Contracts.Bff.Files.UploadFileResponse> response = await Client.UploadFile(fileStream, fileName, contentType);

        if (response.Success)
        {
            Console.WriteLine("Uploaded file {0}/{1} successfully.", response.Content!.File.Bucket, response.Content!.File.Path);
        }
        else
        {
            Console.WriteLine("Uploading file resulted in {0} errors:", response.Errors.Count);
            foreach (string error in response.Errors)
            {
                Console.WriteLine("  {0}", error);
            }
        }

        Console.Write("Press ENTER to continue...");
        Console.ReadLine();
        return States.Files;
    }

    private string GetContentType(string filePath)
    {
        string extension = Path.GetExtension(filePath);
        return _fileUtility.GetContentType(extension);
    }
}
