using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CecoChat.Contracts.History;

namespace CecoChat.Client.Console.Interaction
{
    public abstract class State
    {
        protected StateContainer States { get; }

        protected State(StateContainer states)
        {
            States = states;
        }

        protected MessageStorage Storage => States.Storage;
        protected MessagingClient Client => States.Client;
        protected StateContext Context => States.Context;

        protected async Task GetUserHistory()
        {
            IList<HistoryMessage> history = await Client.GetUserHistory(DateTime.UtcNow);
            foreach (HistoryMessage item in history)
            {
                Message message = CreateMessage(item);
                Storage.AddMessage(message);
            }
        }

        protected async Task GetDialogHistory(long userID)
        {
            IList<HistoryMessage> history = await Client.GetHistory(userID, DateTime.UtcNow);
            foreach (HistoryMessage item in history)
            {
                Message message = CreateMessage(item);
                Storage.AddMessage(message);
            }
        }

        public abstract Task<State> Execute();

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

            switch (historyMessage.Status)
            {
                case Contracts.History.DeliveryStatus.Processed:
                    message.Status = DeliveryStatus.Processed;
                    break;
                case Contracts.History.DeliveryStatus.Delivered:
                    message.Status = DeliveryStatus.Delivered;
                    break;
                case Contracts.History.DeliveryStatus.Seen:
                    message.Status = DeliveryStatus.Seen;
                    break;
                default:
                    throw new EnumValueNotSupportedException(historyMessage.Status);
            }

            foreach (KeyValuePair<long, string> pair in historyMessage.Reactions)
            {
                message.Reactions.Add(pair.Key, pair.Value);
            }

            return message;
        }
    }
}