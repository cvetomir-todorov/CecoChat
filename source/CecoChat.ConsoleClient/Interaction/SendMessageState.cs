using CecoChat.ConsoleClient.LocalStorage;

namespace CecoChat.ConsoleClient.Interaction;

public sealed class SendMessageState : State
{
    public SendMessageState(StateContainer states) : base(states)
    { }

    public override async Task<State> Execute()
    {
        Console.Write("Message: ");
        string? plainText = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(plainText))
        {
            Context.ReloadData = false;
            return States.OneChat;
        }

        long messageId = await MessagingClient.SendPlainTextMessage(Context.UserId, plainText);
        Message message = new()
        {
            MessageId = messageId,
            SenderId = Client.UserId,
            ReceiverId = Context.UserId,
            Text = plainText,
            Type = MessageType.PlainText
        };
        MessageStorage.AddMessage(message);

        Context.ReloadData = false;
        return States.OneChat;
    }
}
