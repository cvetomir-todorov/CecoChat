using CecoChat.Contracts.Messaging;
using CecoChat.Data;
using Common;

namespace CecoChat.ConsoleClient.LocalStorage;

public class ChangeHandler
{
    private readonly long _userId;
    private readonly MessageStorage _messageStorage;
    private readonly ConnectionStorage _connectionStorage;

    public ChangeHandler(long userId, MessageStorage messageStorage, ConnectionStorage connectionStorage)
    {
        _userId = userId;
        _messageStorage = messageStorage;
        _connectionStorage = connectionStorage;
    }

    public void HandlePlainTextMessage(ListenNotification notification)
    {
        if (notification.Type != Contracts.Messaging.MessageType.PlainText)
        {
            throw new InvalidOperationException($"Notification {notification} should have type {Contracts.Messaging.MessageType.PlainText}.");
        }
        if (notification.PlainText == null)
        {
            throw new InvalidOperationException($"Notification {notification} should have its {nameof(ListenNotification.PlainText)} property not null.");
        }

        Message message = new()
        {
            MessageId = notification.MessageId,
            SenderId = notification.SenderId,
            ReceiverId = notification.ReceiverId,
            Type = MessageType.PlainText,
            Text = notification.PlainText.Text
        };

        _messageStorage.AddMessage(message);
    }

    public void HandleFileMessage(ListenNotification notification)
    {
        if (notification.Type != Contracts.Messaging.MessageType.File)
        {
            throw new InvalidOperationException($"Notification {notification} should have type {Contracts.Messaging.MessageType.File}.");
        }
        if (notification.File == null)
        {
            throw new InvalidOperationException($"Notification {notification} should have its {nameof(ListenNotification.File)} property not null.");
        }

        Message message = new()
        {
            MessageId = notification.MessageId,
            SenderId = notification.SenderId,
            ReceiverId = notification.ReceiverId,
            Type = MessageType.File,
            Text = notification.File.Text,
            FileBucket = notification.File.Bucket,
            FilePath = notification.File.Path
        };

        _messageStorage.AddMessage(message);
    }

    public void UpdateDeliveryStatus(ListenNotification notification)
    {
        if (notification.Type != Contracts.Messaging.MessageType.DeliveryStatus)
        {
            throw new InvalidOperationException($"Notification {notification} should have type {Contracts.Messaging.MessageType.DeliveryStatus}.");
        }

        if (!_messageStorage.TryGetChat(notification.SenderId, notification.ReceiverId, out Chat? chat))
        {
            long otherUserId = DataUtility.GetOtherUserId(_userId, notification.SenderId, notification.ReceiverId);
            chat = new Chat(otherUserId)
            {
                NewestMessage = notification.MessageId
            };
            _messageStorage.AddOrUpdateChat(chat);
        }

        switch (notification.DeliveryStatus)
        {
            case DeliveryStatus.Processed:
                chat.Processed = notification.MessageId;
                break;
            case DeliveryStatus.Delivered:
                chat.OtherUserDelivered = notification.MessageId;
                break;
            case DeliveryStatus.Seen:
                chat.OtherUserSeen = notification.MessageId;
                break;
            default:
                throw new EnumValueNotSupportedException(notification.DeliveryStatus);
        }
    }

    public void HandleReaction(ListenNotification notification)
    {
        if (notification.Type != Contracts.Messaging.MessageType.Reaction)
        {
            throw new InvalidOperationException($"Notification {notification} should have type {Contracts.Messaging.MessageType.Reaction}.");
        }
        if (notification.Reaction == null)
        {
            throw new InvalidOperationException($"Notification {notification} should have its {nameof(ListenNotification.Reaction)} property not null.");
        }

        if (!_messageStorage.TryGetMessage(notification.SenderId, notification.ReceiverId, notification.MessageId, out Message? message))
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

    public void HandleConnectionChange(ListenNotification notification)
    {
        if (notification.Type != Contracts.Messaging.MessageType.Connection)
        {
            throw new InvalidOperationException($"Notification {notification} should have type {Contracts.Messaging.MessageType.Connection}.");
        }
        if (notification.Connection == null)
        {
            throw new InvalidOperationException($"Notification {notification} should have its {nameof(ListenNotification.Connection)} property not null.");
        }

        ConnectionStatus status;
        switch (notification.Connection.Status)
        {
            case Contracts.Messaging.ConnectionStatus.NotConnected:
                status = ConnectionStatus.NotConnected;
                break;
            case Contracts.Messaging.ConnectionStatus.Pending:
                status = ConnectionStatus.Pending;
                break;
            case Contracts.Messaging.ConnectionStatus.Connected:
                status = ConnectionStatus.Connected;
                break;
            default:
                throw new EnumValueNotSupportedException(notification.Connection.Status);
        }

        long connectionId = DataUtility.GetOtherUserId(_userId, notification.SenderId, notification.ReceiverId);
        _connectionStorage.UpdateConnection(connectionId, status, notification.Connection.Version);
    }
}
