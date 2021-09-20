using System;
using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.Client;

namespace CecoChat.Server
{
    public interface IClientBackendMapper
    {
        BackplaneMessage MapClientToBackplaneMessage(ClientMessage clientMessage);

        ClientMessage MapBackplaneToClientMessage(BackplaneMessage backplaneMessage);
    }

    public sealed class ClientBackendMapper : IClientBackendMapper
    {
        public BackplaneMessage MapClientToBackplaneMessage(ClientMessage clientMessage)
        {
            BackplaneMessage backplaneMessage = new()
            {
                MessageId = clientMessage.MessageId,
                SenderId = clientMessage.SenderId,
                ReceiverId = clientMessage.ReceiverId,
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

        public ClientMessage MapBackplaneToClientMessage(BackplaneMessage backplaneMessage)
        {
            ClientMessage clientMessage = new()
            {
                MessageId = backplaneMessage.MessageId,
                SenderId = backplaneMessage.SenderId,
                ReceiverId = backplaneMessage.ReceiverId,
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
    }
}
