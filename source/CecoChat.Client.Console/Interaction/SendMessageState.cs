using System.Threading.Tasks;
using CecoChat.Contracts.Client;

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
                return States.Dialog;
            }

            ClientMessage message = await States.Client.SendPlainTextMessage(States.Context.UserID, plainText);
            States.Storage.AddMessage(new ListenResponse {Message = message});

            States.Context.ReloadData = true;
            return States.Dialog;
        }
    }
}