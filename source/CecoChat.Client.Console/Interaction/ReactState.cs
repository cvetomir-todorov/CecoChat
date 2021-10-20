using System.Threading.Tasks;

namespace CecoChat.Client.Console.Interaction
{
    public sealed class ReactState : State
    {
        public ReactState(StateContainer states) : base(states)
        {}

        public override async Task<State> Execute()
        {
            System.Console.Write("Choose message ID ('0' to exit): ");
            string messageIDString = System.Console.ReadLine();

            if (string.IsNullOrWhiteSpace(messageIDString) ||
                !long.TryParse(messageIDString, out long messageID) ||
                messageID == 0)
            {
                Context.ReloadData = true;
                return States.Chat;
            }

            if (!Storage.TryGetMessage(Client.UserID, Context.UserID, messageID, out Message message))
            {
                Context.ReloadData = true;
                return States.Chat;
            }

            if (message.Reactions.ContainsKey(Client.UserID))
            {
                await Client.UnReact(messageID, message.SenderID, message.ReceiverID);
            }
            else
            {
                await Client.React(messageID, message.SenderID, message.ReceiverID);
            }

            Context.ReloadData = true;
            return States.Chat;
        }
    }
}