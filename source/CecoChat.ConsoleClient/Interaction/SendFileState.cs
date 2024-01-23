using CecoChat.ConsoleClient.LocalStorage;

namespace CecoChat.ConsoleClient.Interaction;

public sealed class SendFileState : State
{
    public SendFileState(StateContainer states) : base(states)
    { }

    public override async Task<State> Execute()
    {
        Console.Write("Send a new file (press 'n') or an existing one (press 'e'): ");
        ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
        Console.WriteLine();

        string bucket;
        string path;

        if (keyInfo.KeyChar == 'n')
        {
            UploadFileResult result = await UploadFile();
            if (!result.Success)
            {
                Context.ReloadData = false;
                return States.OneChat;
            }

            bucket = result.Bucket;
            path = result.Path;
        }
        else if (keyInfo.KeyChar == 'e')
        {
            Console.WriteLine("Choose which file to upload:");
            List<FileRef> userFiles = DisplayUserFiles();
            keyInfo = Console.ReadKey(intercept: true);
            int index = keyInfo.KeyChar - '0';
            FileRef chosenFile = userFiles[index];

            bucket = chosenFile.Bucket;
            path = chosenFile.Path;
        }
        else
        {
            Context.ReloadData = false;
            return States.OneChat;
        }

        Console.Write("Text in addition to the file: ");
        string text = Console.ReadLine() ?? string.Empty;

        long messageId = await MessagingClient.SendFileMessage(Context.UserId, text, bucket, path);
        Message message = new()
        {
            MessageId = messageId,
            SenderId = Client.UserId,
            ReceiverId = Context.UserId,
            Text = text,
            Type = MessageType.File,
            FileBucket = bucket,
            FilePath = path
        };
        MessageStorage.AddMessage(message);

        Context.ReloadData = false;
        return States.OneChat;
    }
}
