using CecoChat.ConsoleClient.LocalStorage;

namespace CecoChat.ConsoleClient.Interaction;

public sealed class DownloadSentFileState : State
{
    public DownloadSentFileState(StateContainer states) : base(states)
    { }

    public override async Task<State> Execute()
    {
        Console.Write("Choose message ID (ENTER to exit): ");
        string? messageIdString = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(messageIdString) || !long.TryParse(messageIdString, out long messageId))
        {
            Context.ReloadData = false;
            return States.OneChat;
        }
        if (!MessageStorage.TryGetMessage(Client.UserId, Context.UserId, messageId, out Message? message))
        {
            Context.ReloadData = false;
            return States.OneChat;
        }
        if (string.IsNullOrWhiteSpace(message.FileBucket) || string.IsNullOrWhiteSpace(message.FilePath))
        {
            Console.Write("Chosen message doesn't have a file. Press ENTER to continue...");
            Console.ReadLine();

            Context.ReloadData = false;
            return States.OneChat;
        }

        await DownloadFile(message.FileBucket, message.FilePath);

        Context.ReloadData = false;
        return States.OneChat;
    }
}
