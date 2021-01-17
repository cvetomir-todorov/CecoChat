using System;
using CecoChat.Contracts.Backend;
using CecoChat.Contracts.Client;
using ClientMessage = CecoChat.Contracts.Client.Message;
using BackendMessage = CecoChat.Contracts.Backend.Message;
using ClientMessageType = CecoChat.Contracts.Client.MessageType;
using BackendMessageType = CecoChat.Contracts.Backend.MessageType;

namespace CecoChat.Messaging.Server.Shared
{
    public interface IClientBackendMapper
    {
        BackendMessage MapClientToBackendMessage(ClientMessage clientMessage);

        ClientMessage MapBackendToClientMessage(BackendMessage backendMessage);
    }

    public sealed class ClientBackendMapper : IClientBackendMapper
    {
        public BackendMessage MapClientToBackendMessage(ClientMessage clientMessage)
        {
            BackendMessage backendMessage;
            switch (clientMessage.Type)
            {
                case ClientMessageType.PlainText:
                    backendMessage = new PlainTextMessage
                    {
                        Type = BackendMessageType.PlainText,
                        Text = clientMessage.PlainTextData.Text
                    };
                    break;
                default:
                    throw new NotSupportedException($"{typeof(ClientMessageType).FullName} value {clientMessage.Type} is not supported.");
            }

            backendMessage.MessageID = clientMessage.MessageId;
            backendMessage.SenderID = clientMessage.SenderId;
            backendMessage.ReceiverID = clientMessage.ReceiverId;

            return backendMessage;
        }

        public ClientMessage MapBackendToClientMessage(BackendMessage backendMessage)
        {
            ClientMessage clientMessage = new()
            {
                MessageId = backendMessage.MessageID,
                SenderId = backendMessage.SenderID,
                ReceiverId = backendMessage.ReceiverID,
            };

            switch (backendMessage.Type)
            {
                case BackendMessageType.PlainText:
                    clientMessage.Type = ClientMessageType.PlainText;
                    PlainTextMessage plainTextMessage = (PlainTextMessage) backendMessage;
                    clientMessage.PlainTextData = new PlainTextData
                    {
                        Text = plainTextMessage.Text
                    };
                    break;
                default:
                    throw new NotSupportedException($"{typeof(BackendMessageType).FullName} value {backendMessage.Type} is not supported.");
            }

            return clientMessage;
        }
    }
}
