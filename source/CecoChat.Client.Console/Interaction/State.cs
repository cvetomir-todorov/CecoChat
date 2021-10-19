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

        protected async Task GetUserHistory()
        {
            IList<HistoryMessage> history = await States.Client.GetUserHistory(DateTime.UtcNow);
            foreach (HistoryMessage item in history)
            {
                Message message = CreateMessage(item);
                States.Storage.AddMessage(message);
            }
        }

        protected async Task GetDialogHistory(long userID)
        {
            IList<HistoryMessage> history = await States.Client.GetHistory(userID, DateTime.UtcNow);
            foreach (HistoryMessage item in history)
            {
                Message message = CreateMessage(item);
                States.Storage.AddMessage(message);
            }
        }

        public abstract Task<State> Execute();

        private static Message CreateMessage(HistoryMessage item)
        {
            Message message = new()
            {
                MessageID = item.MessageId,
                SenderID = item.SenderId,
                ReceiverID = item.ReceiverId,
            };

            switch (item.DataType)
            {
                case Contracts.History.DataType.PlainText:
                    message.DataType = DataType.PlainText;
                    message.Data = item.Data;
                    break;
                default:
                    throw new EnumValueNotSupportedException(item.DataType);
            }

            switch (item.Status)
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
                    throw new EnumValueNotSupportedException(item.Status);
            }

            foreach (KeyValuePair<long, string> pair in item.Reactions)
            {
                message.Reactions.Add(pair.Key, pair.Value);
            }

            return message;
        }
    }
}