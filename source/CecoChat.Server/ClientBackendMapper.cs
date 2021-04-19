﻿using System;
using CecoChat.Contracts.Backend;
using CecoChat.Contracts.Client;

namespace CecoChat.Server
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
            BackendMessage backendMessage = new()
            {
                MessageId = clientMessage.MessageId,
                SenderId = clientMessage.SenderId,
                ReceiverId = clientMessage.ReceiverId,
            };

            switch (clientMessage.Type)
            {
                case ClientMessageType.PlainText:
                    backendMessage.Type = BackendMessageType.PlainText;
                    backendMessage.Text = clientMessage.Text;
                    break;
                default:
                    throw new NotSupportedException($"{typeof(ClientMessageType).FullName} value {clientMessage.Type} is not supported.");
            }

            return backendMessage;
        }

        public ClientMessage MapBackendToClientMessage(BackendMessage backendMessage)
        {
            ClientMessage clientMessage = new()
            {
                MessageId = backendMessage.MessageId,
                SenderId = backendMessage.SenderId,
                ReceiverId = backendMessage.ReceiverId,
            };

            switch (backendMessage.Type)
            {
                case BackendMessageType.PlainText:
                    clientMessage.Type = ClientMessageType.PlainText;
                    clientMessage.Text = backendMessage.Text;
                    break;
                default:
                    throw new NotSupportedException($"{typeof(BackendMessageType).FullName} value {backendMessage.Type} is not supported.");
            }

            return clientMessage;
        }
    }
}
