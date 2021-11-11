using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CecoChat.Contracts.History;
using CecoChat.Data;

namespace CecoChat.Client.Console.Interaction
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
            IList<Contracts.State.ChatState> chatStates = await Client.GetChats(_lastKnownChatState);

            foreach (Contracts.State.ChatState chatState in chatStates)
            {
                Chat chat = CreateChat(chatState);
                Storage.AddOrUpdateChat(chat);
            }

            _lastKnownChatState = currentState;
        }

        protected async Task GetHistory(long userID)
        {
            IList<HistoryMessage> history = await Client.GetHistory(userID, DateTime.UtcNow);
            foreach (HistoryMessage item in history)
            {
                Message message = CreateMessage(item);
                Storage.AddMessage(message);
            }
        }

        public abstract Task<State> Execute();

        private Chat CreateChat(Contracts.State.ChatState chatState)
        {
            long otherUserID = DataUtility.GetOtherUsedID(chatState.ChatId, Client.UserID);
            Chat chat = new(otherUserID)
            {
                NewestMessage = chatState.NewestMessage,
                OtherUserDelivered = chatState.OtherUserDelivered,
                OtherUserSeen = chatState.OtherUserSeen
            };

            return chat;
        }

        private static Message CreateMessage(HistoryMessage historyMessage)
        {
            Message message = new()
            {
                MessageID = historyMessage.MessageId,
                SenderID = historyMessage.SenderId,
                ReceiverID = historyMessage.ReceiverId,
            };

            switch (historyMessage.DataType)
            {
                case Contracts.History.DataType.PlainText:
                    message.DataType = DataType.PlainText;
                    message.Data = historyMessage.Data;
                    break;
                default:
                    throw new EnumValueNotSupportedException(historyMessage.DataType);
            }

            foreach (KeyValuePair<long, string> pair in historyMessage.Reactions)
            {
                message.Reactions.Add(pair.Key, pair.Value);
            }

            return message;
        }
    }
}