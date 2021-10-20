using System.Threading.Tasks;

namespace CecoChat.Client.Console.Interaction
{
    public sealed class SendMessageState : State
    {
        public SendMessageState(StateContainer states) : base(states)
        {}

        public override async Task<State> Execute()
        {
            System.Console.Write("Message to ID={0}: ", States.Context.UserID);
            string plainText = System.Console.ReadLine();
            if (string.IsNullOrWhiteSpace(plainText))
            {
                return States.Chat;
            }

            long messageID = await States.Client.SendPlainTextMessage(States.Context.UserID, plainText);
            Message message = new()
            {
                MessageID = messageID,
                SenderID = States.Client.UserID,
                ReceiverID = States.Context.UserID,
                DataType = DataType.PlainText,
                Data = plainText,
                Status = DeliveryStatus.Unprocessed
            };
            States.Storage.AddMessage(message);

            States.Context.ReloadData = true;
            return States.Chat;
        }
    }
}