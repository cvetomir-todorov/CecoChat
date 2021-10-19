using System;
using CecoChat.Contracts;
using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.History;
using CecoChat.Contracts.Messaging;

namespace CecoChat.Server
{
    public interface IContractDataMapper
    {
        BackplaneMessage CreateBackplaneMessage(SendMessageRequest request, Guid senderClientID, long messageID);

        BackplaneMessage CreateBackplaneMessage(ReactRequest request, Guid senderClientID, long reactorID);

        BackplaneMessage CreateBackplaneMessage(UnReactRequest request, Guid senderClientID, long reactorID);

        ListenNotification CreateListenNotification(SendMessageRequest request, long messageID);

        ListenNotification CreateListenNotification(BackplaneMessage backplaneMessage);

        DataMessage CreateDataMessage(BackplaneMessage backplaneMessage);

        ReactionMessage CreateReactionMessage(BackplaneMessage backplaneMessage);
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
                ClientId = senderClientID.ToUuid(),
                Type = Contracts.Backplane.MessageType.Data,
                Status = Contracts.Backplane.DeliveryStatus.Processed
            };

            switch (request.DataType)
            {
                case Contracts.Messaging.DataType.PlainText:
                    message.Data = new BackplaneData
                    {
                        Type = Contracts.Backplane.DataType.PlainText,
                        Data = request.Data
                    };
                    break;
                default:
                    throw new EnumValueNotSupportedException(request.DataType);
            }

            return message;
        }

        public BackplaneMessage CreateBackplaneMessage(ReactRequest request, Guid senderClientID, long reactorID)
        {
            BackplaneMessage message = new()
            {
                MessageId = request.MessageId,
                SenderId = request.SenderId,
                ReceiverId = request.ReceiverId,
                ClientId = senderClientID.ToUuid(),
                Type = Contracts.Backplane.MessageType.Reaction,
                Reaction = new BackplaneReaction
                {
                    ReactorId = reactorID,
                    Reaction = request.Reaction
                }
            };

            return message;
        }

        public BackplaneMessage CreateBackplaneMessage(UnReactRequest request, Guid senderClientID, long reactorID)
        {
            BackplaneMessage message = new()
            {
                MessageId = request.MessageId,
                SenderId = request.SenderId,
                ReceiverId = request.ReceiverId,
                ClientId = senderClientID.ToUuid(),
                Type = Contracts.Backplane.MessageType.Reaction,
                Reaction = new BackplaneReaction
                {
                    ReactorId = reactorID
                }
            };

            return message;
        }

        public ListenNotification CreateListenNotification(SendMessageRequest request, long messageID)
        {
            ListenNotification notification = new()
            {
                MessageId = messageID,
                SenderId = request.SenderId,
                ReceiverId = request.ReceiverId,
                Type = Contracts.Messaging.MessageType.Data
            };

            switch (request.DataType)
            {
                case Contracts.Messaging.DataType.PlainText:
                    notification.Data = new NotificationData
                    {
                        Type = Contracts.Messaging.DataType.PlainText,
                        Data = request.Data
                    };
                    break;
                default:
                    throw new EnumValueNotSupportedException(request.DataType);
            }

            return notification;
        }

        public ListenNotification CreateListenNotification(BackplaneMessage backplaneMessage)
        {
            ListenNotification notification = new()
            {
                MessageId = backplaneMessage.MessageId,
                SenderId = backplaneMessage.SenderId,
                ReceiverId = backplaneMessage.ReceiverId,
            };

            switch (backplaneMessage.Type)
            {
                case Contracts.Backplane.MessageType.Data:
                    SetData(backplaneMessage, notification);
                    break;
                case Contracts.Backplane.MessageType.Reaction:
                    SetReaction(backplaneMessage, notification);
                    break;
                default:
                    throw new EnumValueNotSupportedException(backplaneMessage.Type);
            }

            return notification;
        }

        private static void SetData(BackplaneMessage backplaneMessage, ListenNotification notification)
        {
            notification.Type = Contracts.Messaging.MessageType.Data;
            switch (backplaneMessage.Data.Type)
            {
                case Contracts.Backplane.DataType.PlainText:
                    notification.Data = new NotificationData
                    {
                        Type = Contracts.Messaging.DataType.PlainText,
                        Data = backplaneMessage.Data.Data
                    };
                    break;
                default:
                    throw new EnumValueNotSupportedException(backplaneMessage.Data.Type);
            }
        }

        private static void SetReaction(BackplaneMessage backplaneMessage, ListenNotification notification)
        {
            notification.Type = Contracts.Messaging.MessageType.Reaction;
            notification.Reaction = new NotificationReaction
            {
                ReactorId = backplaneMessage.Reaction.ReactorId,
                Reaction = backplaneMessage.Reaction.Reaction
            };
        }

        public DataMessage CreateDataMessage(BackplaneMessage backplaneMessage)
        {
            if (backplaneMessage.Type != Contracts.Backplane.MessageType.Data)
            {
                throw new ArgumentException($"Message should be of type {Contracts.Backplane.MessageType.Data}.");
            }

            DataMessage dataMessage = new()
            {
                MessageId = backplaneMessage.MessageId,
                SenderId = backplaneMessage.SenderId,
                ReceiverId = backplaneMessage.ReceiverId
            };

            switch (backplaneMessage.Data.Type)
            {
                case Contracts.Backplane.DataType.PlainText:
                    dataMessage.DataType = Contracts.History.DataType.PlainText;
                    dataMessage.Data = backplaneMessage.Data.Data;
                    break;
                default:
                    throw new EnumValueNotSupportedException(backplaneMessage.Data.Type);
            }

            return dataMessage;
        }

        public ReactionMessage CreateReactionMessage(BackplaneMessage backplaneMessage)
        {
            if (backplaneMessage.Type != Contracts.Backplane.MessageType.Reaction)
            {
                throw new ArgumentException($"Message should be of type {Contracts.Backplane.MessageType.Reaction}.");
            }

            ReactionMessage reactionMessage = new()
            {
                MessageId = backplaneMessage.MessageId,
                SenderId = backplaneMessage.SenderId,
                ReceiverId = backplaneMessage.ReceiverId,
                ReactorId = backplaneMessage.Reaction.ReactorId,
                Reaction = backplaneMessage.Reaction.Reaction,
                Type = string.IsNullOrWhiteSpace(backplaneMessage.Reaction.Reaction) ? NewReactionType.Unset : NewReactionType.Set
            };

            return reactionMessage;
        }
    }
}