using System.Threading.Tasks;
using CecoChat.Client.Console.LocalStorage;

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
                return States.OneChat;
            }

            if (!Storage.TryGetMessage(Client.UserID, Context.UserID, messageID, out Message message))
            {
                Context.ReloadData = true;
                return States.OneChat;
            }

            if (message.Reactions.ContainsKey(Client.UserID))
            {
                await Client.UnReact(messageID, message.SenderID, message.ReceiverID);
                message.Reactions.Remove(Client.UserID);
            }
            else
            {
                string reaction = Reactions.ThumbsUp;
                await Client.React(messageID, message.SenderID, message.ReceiverID, reaction);
                message.Reactions.Add(Client.UserID, reaction);
            }

            Context.ReloadData = true;
            return States.OneChat;
        }

        private static class Reactions
        {
            public static readonly string ThumbsUp = "\\u1F44D";
        }
    }
}