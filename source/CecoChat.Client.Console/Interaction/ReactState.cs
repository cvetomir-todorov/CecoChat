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
                States.Context.ReloadData = true;
                return States.Chat;
            }

            if (!States.Storage.TryGetMessage(States.Client.UserID, States.Context.UserID, messageID, out Message message))
            {
                States.Context.ReloadData = true;
                return States.Chat;
            }

            if (message.Reactions.ContainsKey(States.Client.UserID))
            {
                await States.Client.UnReact(messageID, message.SenderID, message.ReceiverID);
            }
            else
            {
                await States.Client.React(messageID, message.SenderID, message.ReceiverID);
            }

            States.Context.ReloadData = true;
            return States.Chat;
        }
    }
}