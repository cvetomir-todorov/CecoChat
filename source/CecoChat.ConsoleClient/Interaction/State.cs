using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CecoChat.ConsoleClient.Api;
using CecoChat.ConsoleClient.LocalStorage;

namespace CecoChat.ConsoleClient.Interaction
{
    public abstract class State
    {
        private DateTime _lastKnownChatState;
        protected StateContainer States { get; }

        protected State(StateContainer states)
        {
            _lastKnownChatState = Snowflake.Epoch;
            States = states;
        }

        protected MessageStorage Storage => States.Storage;
        protected MessagingClient Client => States.Client;
        protected StateContext Context => States.Context;

        protected async Task GetChats()
        {
            DateTime currentState = DateTime.UtcNow;
            IList<Chat> chats = await Client.GetChats(_lastKnownChatState);

            foreach (Chat chat in chats)
            {
                Storage.AddOrUpdateChat(chat);
            }

            _lastKnownChatState = currentState;
        }

        protected async Task GetHistory(long userID)
        {
            IList<Message> history = await Client.GetHistory(userID, DateTime.UtcNow);
            foreach (Message message in history)
            {
                Storage.AddMessage(message);
            }
        }

        public abstract Task<State> Execute();
    }
}