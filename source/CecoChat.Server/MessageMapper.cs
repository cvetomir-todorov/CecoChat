using System;
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
                    throw new NotSupportedException($"{typeof(ClientMessageType).FullName} value {clientMessage.Type} is not supported.");
            }

            return backplaneMessage;
        }

        public HistoryMessage MapBackplaneToHistoryMessage(BackplaneMessage backplaneMessage)
        {
            HistoryMessage historyMessage = new()
            {
                MessageID = backplaneMessage.MessageId,
                SenderID = backplaneMessage.SenderId,
                ReceiverID = backplaneMessage.ReceiverId
            };

            switch (backplaneMessage.Type)
            {
                case BackplaneMessageType.PlainText:
                    historyMessage.Type = HistoryMessageType.PlainText;
                    historyMessage.Text = backplaneMessage.Text;
                    break;
                default:
                    throw new NotSupportedException($"{typeof(BackplaneMessageType).FullName} value {backplaneMessage.Type} is not supported.");
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
                    throw new NotSupportedException($"{typeof(BackplaneMessageType).FullName} value {backplaneMessage.Type} is not supported.");
            }

            return clientMessage;
        }

        public ClientMessage MapHistoryToClientMessage(HistoryMessage historyMessage)
        {
            ClientMessage clientMessage = new()
            {
                MessageId = historyMessage.MessageID,
                SenderId = historyMessage.SenderID,
                ReceiverId = historyMessage.ReceiverID,
            };

            switch (historyMessage.Type)
            {
                case HistoryMessageType.PlainText:
                    clientMessage.Type = ClientMessageType.PlainText;
                    clientMessage.Text = historyMessage.Text;
                    break;
                default:
                    throw new NotSupportedException($"{typeof(HistoryMessageType).FullName} value {historyMessage.Type} is not supported.");
            }

            return clientMessage;
        }
    }
}
