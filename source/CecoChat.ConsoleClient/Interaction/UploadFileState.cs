using CecoChat.ConsoleClient.Api;

namespace CecoChat.ConsoleClient.Interaction;

public class UploadFileState : State
{
    private readonly IDictionary<string, string> _contentTypes;
    private readonly string _defaultContentType;

    public UploadFileState(StateContainer states) : base(states)
    {
        _contentTypes = new Dictionary<string, string>();
        
        _contentTypes.Add(".png", "image/png");
        _contentTypes.Add(".jpg", "image/jpg");
        _contentTypes.Add(".jpeg", "image/jpeg");
        _contentTypes.Add(".txt", "text/plain");

        _defaultContentType = "application/octet-stream";
    }

    public override async Task<State> Execute()
    {
        Console.Clear();
        DisplayUserData();
        DisplaySplitter();
        DisplayUserFiles();

        Console.Write("Enter the path to the file to be uploaded or leave empty to exit: ");
        string? filePath = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Context.ReloadData = true;
            return States.AllChats;
        }

        if (!File.Exists(filePath))
        {
            Console.WriteLine("File {0} doesn't exist, press ENTER to exit", filePath);
            Console.ReadLine();

            Context.ReloadData = true;
            return States.AllChats;
        }

        string contentType = GetContentType(filePath);

        Console.WriteLine("Sending file {0} with content type {1}...", filePath, contentType);
        ClientResponse<Contracts.Bff.Files.UploadFileResponse> response = await Client.UploadFile(filePath, contentType);

        if (response.Success)
        {
            Console.WriteLine("Uploaded file to {0}/{1} successfully!", response.Content!.File.Bucket, response.Content!.File.Path);
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
        return States.AllChats;
    }

    private string GetContentType(string filePath)
    {
        string extension = Path.GetExtension(filePath);
        if (!_contentTypes.TryGetValue(extension, out string? contentType))
        {
            contentType = _defaultContentType;
        }

        return contentType;
    }
}
