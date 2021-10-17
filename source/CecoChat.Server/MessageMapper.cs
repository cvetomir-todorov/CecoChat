using System.Collections.Generic;
using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.Client;
using CecoChat.Contracts.History;

namespace CecoChat.Server
{
    public interface IMessageMapper
    {
        BackplaneMessage MapClientToBackplaneMessage(ClientMessage clientMessage);

        HistoryMessage MapBackplaneToHistoryMessage(BackplaneMessage backplaneMessage);

        ClientMessage MapBackplaneToClientMessage(BackplaneMessage backplaneMessage);

        ClientMessage MapHistoryToClientMessage(HistoryMessage historyMessage);
    }

    public sealed class MessageMapper : IMessageMapper
    {
        public BackplaneMessage MapClientToBackplaneMessage(ClientMessage clientMessage)
        {
            BackplaneMessage backplaneMessage = new()
            {
                MessageId = clientMessage.MessageId,
                SenderId = clientMessage.SenderId,
                ReceiverId = clientMessage.ReceiverId
            };

            switch (clientMessage.Type)
            {
                case ClientMessageType.PlainText:
                    backplaneMessage.Type = BackplaneMessageType.PlainText;
                    backplaneMessage.Text = clientMessage.Text;
                    break;
                default:
                    throw new EnumValueNotSupportedException(clientMessage.Type);
            }

            // ignore status

            return backplaneMessage;
        }

        public HistoryMessage MapBackplaneToHistoryMessage(BackplaneMessage backplaneMessage)
        {
            HistoryMessage historyMessage = new()
            {
                MessageId = backplaneMessage.MessageId,
                SenderId = backplaneMessage.SenderId,
                ReceiverId = backplaneMessage.ReceiverId
            };

            switch (backplaneMessage.Type)
            {
                case BackplaneMessageType.PlainText:
                    historyMessage.Type = HistoryMessageType.PlainText;
                    historyMessage.Text = backplaneMessage.Text;
                    break;
                default:
                    throw new EnumValueNotSupportedException(backplaneMessage.Type);
            }

            switch (backplaneMessage.Status)
            {
                case BackplaneMessageStatus.Processed:
                    historyMessage.Status = HistoryMessageStatus.Processed;
                    break;
                case BackplaneMessageStatus.Delivered:
                    historyMessage.Status = HistoryMessageStatus.Delivered;
                    break;
                default:
                    throw new EnumValueNotSupportedException(backplaneMessage.Status);
            }

            return historyMessage;
        }

        public ClientMessage MapBackplaneToClientMessage(BackplaneMessage backplaneMessage)
        {
            ClientMessage clientMessage = new()
            {
                MessageId = backplaneMessage.MessageId,
                SenderId = backplaneMessage.SenderId,
                ReceiverId = backplaneMessage.ReceiverId
            };

            switch (backplaneMessage.Type)
            {
                case BackplaneMessageType.PlainText:
                    clientMessage.Type = ClientMessageType.PlainText;
                    clientMessage.Text = backplaneMessage.Text;
                    break;
                default:
                    throw new EnumValueNotSupportedException(backplaneMessage.Type);
            }

            switch (backplaneMessage.Status)
            {
                case BackplaneMessageStatus.Processed:
                    clientMessage.Status = ClientMessageStatus.Processed;
                    break;
                case BackplaneMessageStatus.Delivered:
                    clientMessage.Status = ClientMessageStatus.Delivered;
                    break;
                default:
                    throw new EnumValueNotSupportedException(backplaneMessage.Status);
            }

            return clientMessage;
        }

        public ClientMessage MapHistoryToClientMessage(HistoryMessage historyMessage)
        {
            ClientMessage clientMessage = new()
            {
                MessageId = historyMessage.MessageId,
                SenderId = historyMessage.SenderId,
                ReceiverId = historyMessage.ReceiverId
            };

            switch (historyMessage.Type)
            {
                case HistoryMessageType.PlainText:
                    clientMessage.Type = ClientMessageType.PlainText;
                    clientMessage.Text = historyMessage.Text;
                    break;
                default:
                    throw new EnumValueNotSupportedException(historyMessage.Type);
            }

            switch (historyMessage.Status)
            {
                case HistoryMessageStatus.Processed:
                    clientMessage.Status = ClientMessageStatus.Processed;
                    break;
                case HistoryMessageStatus.Delivered:
                    clientMessage.Status = ClientMessageStatus.Delivered;
                    break;
                default:
                    throw new EnumValueNotSupportedException(historyMessage.Status);
            }

            foreach (KeyValuePair<long, string> pair in historyMessage.Reactions)
            {
                clientMessage.Reactions.Add(pair.Key, pair.Value);
            }

            return clientMessage;
        }
    }
}
