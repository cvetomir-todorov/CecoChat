using CecoChat.Contracts.Messaging;
using CecoChat.Data;

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

    public void AddReceivedMessage(ListenNotification notification)
    {
        if (notification.Type != MessageType.Data)
        {
            throw new InvalidOperationException($"Notification {notification} should have type {MessageType.Data}.");
        }
        if (notification.Data == null)
        {
            throw new InvalidOperationException($"Notification {notification} should have its {nameof(ListenNotification.Data)} property not null.");
        }

        Message message = new()
        {
            MessageId = notification.MessageId,
            SenderId = notification.SenderId,
            ReceiverId = notification.ReceiverId
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

        _messageStorage.AddMessage(message);
    }

    public void UpdateDeliveryStatus(ListenNotification notification)
    {
        if (notification.Type != MessageType.DeliveryStatus)
        {
            throw new InvalidOperationException($"Notification {notification} should have type {MessageType.DeliveryStatus}.");
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
        if (notification.Type != MessageType.Reaction)
        {
            throw new InvalidOperationException($"Notification {notification} should have type {MessageType.Reaction}.");
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
        if (notification.Type != MessageType.Connection)
        {
            throw new InvalidOperationException($"Notification {notification} should have type {MessageType.Connection}.");
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
