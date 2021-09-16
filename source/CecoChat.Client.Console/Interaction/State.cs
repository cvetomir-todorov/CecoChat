using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CecoChat.Client.Shared;
using CecoChat.Contracts.Client;

namespace CecoChat.Client.Console.Interaction
{
    public sealed class StateContext
    {
        public bool ReloadData { get; set; }
        public long UserID { get; set; }
    }

    // ReSharper disable once ArrangeModifiersOrder
    public abstract class State
    {
        protected MessagingClient Client { get; }
        protected MessageStorage Storage { get; }
        protected StateContainer States { get; }

        protected State(StateContainer states, MessagingClient client, MessageStorage storage)
        {
            States = states;
            Client = client;
            Storage = storage;
        }

        protected async Task GetUserHistory()
        {
            IList<ClientMessage> messages = await Client.GetUserHistory(DateTime.UtcNow);
            foreach (ClientMessage message in messages)
            {
                Storage.AddMessage(new ListenResponse {Message = message});
            }
        }

        protected async Task GetDialogHistory(long userID)
        {
            IList<ClientMessage> messages = await Client.GetDialogHistory(userID, DateTime.UtcNow);
            foreach (ClientMessage message in messages)
            {
                Storage.AddMessage(new ListenResponse {Message = message});
            }
        }

        // ReSharper disable once ArrangeModifiersOrder
        public abstract Task<State> Execute(StateContext context);
    }
}