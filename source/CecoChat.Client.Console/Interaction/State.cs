using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CecoChat.Contracts.Client;

namespace CecoChat.Client.Console.Interaction
{
    public abstract class State
    {
        protected StateContainer States { get; }

        protected State(StateContainer states)
        {
            States = states;
        }

        protected async Task GetUserHistory()
        {
            IList<ClientMessage> messages = await States.Client.GetUserHistory(DateTime.UtcNow);
            foreach (ClientMessage message in messages)
            {
                States.Storage.AddMessage(new ListenResponse {Message = message});
            }
        }

        protected async Task GetDialogHistory(long userID)
        {
            IList<ClientMessage> messages = await States.Client.GetDialogHistory(userID, DateTime.UtcNow);
            foreach (ClientMessage message in messages)
            {
                States.Storage.AddMessage(new ListenResponse {Message = message});
            }
        }

        public abstract Task<State> Execute();
    }
}