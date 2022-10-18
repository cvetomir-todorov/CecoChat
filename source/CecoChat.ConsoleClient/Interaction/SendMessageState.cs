using CecoChat.ConsoleClient.LocalStorage;

namespace CecoChat.ConsoleClient.Interaction;

public sealed class SendMessageState : State
{
    public SendMessageState(StateContainer states) : base(states)
    { }

    public override async Task<State> Execute()
    {
        Console.Write("Message to ID={0}: ", Context.UserID);
        string plainText = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(plainText))
        {
            Context.ReloadData = false;
            return States.OneChat;
        }

        long messageID = await Client.SendPlainTextMessage(Context.UserID, plainText);
        Message message = new()
        {
            MessageID = messageID,
            SenderID = Client.UserID,
            ReceiverID = Context.UserID,
            DataType = DataType.PlainText,
            Data = plainText,
        };
        Storage.AddMessage(message);

        Context.ReloadData = false;
        return States.OneChat;
    }
}