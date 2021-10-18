using System;
using System.Collections.Generic;
using CecoChat.Contracts;
using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.History;
using CecoChat.Contracts.Messaging;
using DataType = CecoChat.Contracts.Messaging.DataType;
using DeliveryStatus = CecoChat.Contracts.History.DeliveryStatus;

namespace CecoChat.Server
{
    public interface IContractDataMapper
    {
        BackplaneMessage CreateBackplaneMessage(SendMessageRequest request, Guid senderClientID, long messageID);
        
        ListenResponse CreateListenResponse(SendMessageRequest request, long messageID);

        ListenResponse CreateListenResponse(BackplaneMessage message);

        HistoryItem CreateHistoryItem(HistoryMessage message);
    }

    public class ContractDataMapper : IContractDataMapper
    {
        public BackplaneMessage CreateBackplaneMessage(SendMessageRequest request, Guid senderClientID, long messageID)
        {
            BackplaneMessage message = new()
            {
                MessageId = messageID,
                SenderId = request.SenderId,
                ReceiverId = request.ReceiverId,
                Status = BackplaneMessageStatus.Processed,
                ClientId = senderClientID.ToUuid()
            };

            switch (request.DataType)
            {
                case DataType.PlainText:
                    message.Type = BackplaneMessageType.PlainText;
                    message.Text = request.Data;
                    break;
                default:
                    throw new EnumValueNotSupportedException(request.DataType);
            }

            return message;
        }

        public ListenResponse CreateListenResponse(SendMessageRequest request, long messageID)
        {
            ListenResponse response = new()
            {
                MessageId = messageID,
                SenderId = request.SenderId,
                ReceiverId = request.ReceiverId,
                Type = MessageType.Data,
                MessageData = new()
            };

            switch (request.DataType)
            {
                case DataType.PlainText:
                    response.MessageData.Type = DataType.PlainText;
                    response.MessageData.Data = request.Data;
                    break;
                default:
                    throw new EnumValueNotSupportedException(request.DataType);
            }

            return response;
        }

        public ListenResponse CreateListenResponse(BackplaneMessage message)
        {
            ListenResponse response = new()
            {
                MessageId = message.MessageId,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
            };

            switch (message.Type)
            {
                case BackplaneMessageType.PlainText:
                    response.Type = MessageType.Data;
                    response.MessageData = new()
                    {
                        Type = DataType.PlainText,
                        Data = message.Text
                    };
                    break;
                default:
                    throw new EnumValueNotSupportedException(message.Type);
            }

            return response;
        }

        public HistoryItem CreateHistoryItem(HistoryMessage message)
        {
            HistoryItem item = new()
            {
                MessageId = message.MessageId,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
            };

            switch (message.Type)
            {
                case HistoryMessageType.PlainText:
                    item.DataType = Contracts.History.DataType.PlainText;
                    item.Data = message.Text;
                    break;
                default:
                    throw new EnumValueNotSupportedException(message.Type);
            }

            switch (message.Status)
            {
                case HistoryMessageStatus.Processed:
                    item.Status = DeliveryStatus.Processed;
                    break;
                case HistoryMessageStatus.Delivered:
                    item.Status = DeliveryStatus.Delivered;
                    break;
                default:
                    throw new EnumValueNotSupportedException(message.Status);
            }

            foreach (KeyValuePair<long,string> pair in message.Reactions)
            {
                item.Reactions.Add(pair.Key, pair.Value);
            }

            return item;
        }
    }
}