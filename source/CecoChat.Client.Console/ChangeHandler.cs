using System;
using CecoChat.Contracts.Messaging;

namespace CecoChat.Client.Console
{
    public class ChangeHandler
    {
        private readonly MessageStorage _storage;

        public ChangeHandler(MessageStorage storage)
        {
            _storage = storage;
        }
        
        public void AddReceivedMessage(ListenNotification notification)
        {
            if (notification.Type != MessageType.Data)
            {
                throw new InvalidOperationException($"Notification {notification} should have type {MessageType.Data}.");
            }

            Message message = new()
            {
                MessageID = notification.MessageId,
                SenderID = notification.SenderId,
                ReceiverID = notification.ReceiverId,
                SequenceNumber = notification.SequenceNumber,
                Status = DeliveryStatus.Processed
            };

            switch (notification.Data.Type)
            {
                case Contracts.Messaging.DataType.PlainText:
                    message.DataType = DataType.PlainText;
                    message.Data = notification.Data.Data;
                    break;
                default:
                    throw new EnumValueNotSupportedException(notification.Data.Type);
            }

            _storage.AddMessage(message);
        }

        public void UpdateDeliveryStatus(ListenNotification notification)
        {
            if (notification.Type != MessageType.Delivery)
            {
                throw new InvalidOperationException($"Notification {notification} should have type {MessageType.Delivery}.");
            }

            if (!_storage.TryGetMessage(notification.SenderId, notification.ReceiverId, notification.MessageId, out Message message))
            {
                // the message is not in the local history
                return;
            }

            switch (notification.Status)
            {
                case Contracts.Messaging.DeliveryStatus.Lost:
                    message.Status = DeliveryStatus.Lost;
                    break;
                case Contracts.Messaging.DeliveryStatus.Processed:
                    message.Status = DeliveryStatus.Processed;
                    break;
                case Contracts.Messaging.DeliveryStatus.Delivered:
                    message.Status = DeliveryStatus.Delivered;
                    break;
                case Contracts.Messaging.DeliveryStatus.Seen:
                    message.Status = DeliveryStatus.Seen;
                    break;
                default:
                    throw new EnumValueNotSupportedException(notification.Status);
            }
        }

        public void UpdateReaction(ListenNotification notification)
        {
            if (notification.Type != MessageType.Reaction)
            {
                throw new InvalidOperationException($"Notification {notification} should have type {MessageType.Reaction}.");
            }

            if (!_storage.TryGetMessage(notification.SenderId, notification.ReceiverId, notification.MessageId, out Message message))
            {
                // the message is not in the local history
                return;
            }

            if (string.IsNullOrWhiteSpace(notification.Reaction.Reaction))
            {
                if (message.Reactions.ContainsKey(notification.Reaction.ReactorId))
                {
                    message.Reactions.Remove(notification.Reaction.ReactorId);
                }
            }
            else
            {
                message.Reactions.Add(notification.Reaction.ReactorId, notification.Reaction.Reaction);
            }
        }
    }
}