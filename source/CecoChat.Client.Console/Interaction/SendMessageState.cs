using System.Threading.Tasks;
using CecoChat.Client.Shared;
using CecoChat.Contracts.Client;

namespace CecoChat.Client.Console.Interaction
{
    public sealed class SendMessageState : State
    {
        public SendMessageState(StateContainer states, MessagingClient client, MessageStorage storage) : base(states, client, storage)
        {}

        public override async Task<State> Execute(StateContext context)
        {
            System.Console.Write("Message to ID={0}: ", context.UserID);
            string plainText = System.Console.ReadLine();
            if (string.IsNullOrWhiteSpace(plainText))
            {
                return States.Dialog;
            }

            ClientMessage message = await Client.SendPlainTextMessage(context.UserID, plainText);
            Storage.AddMessage(new ListenResponse {Message = message});

            context.ReloadData = true;
            return States.Dialog;
        }
    }
}