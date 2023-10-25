using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.Messaging;

namespace CecoChat.Server.Messaging;

public interface IContractMapper
{
    BackplaneMessage CreateBackplaneMessage(SendMessageRequest request, long senderId, string initiatorConnection, long messageId);

    BackplaneMessage CreateBackplaneMessage(ReactRequest request, string initiatorConnection, long reactorId);

    BackplaneMessage CreateBackplaneMessage(UnReactRequest request, string initiatorConnection, long reactorId);

    ListenNotification CreateListenNotification(SendMessageRequest request, long senderId, long messageId);

    ListenNotification CreateListenNotification(ReactRequest request, long reactorId);

    ListenNotification CreateListenNotification(UnReactRequest request, long reactorId);

    ListenNotification CreateListenNotification(BackplaneMessage backplaneMessage);
}

public class ContractMapper : IContractMapper
{
    public BackplaneMessage CreateBackplaneMessage(SendMessageRequest request, long senderId, string initiatorConnection, long messageId)
    {
        BackplaneMessage message = new()
        {
            MessageId = messageId,
            SenderId = senderId,
            ReceiverId = request.ReceiverId,
            InitiatorConnection = initiatorConnection,
            TargetUserId = request.ReceiverId,
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

    public BackplaneMessage CreateBackplaneMessage(ReactRequest request, string initiatorConnection, long reactorId)
    {
        BackplaneMessage message = new()
        {
            MessageId = request.MessageId,
            SenderId = request.SenderId,
            ReceiverId = request.ReceiverId,
            InitiatorConnection = initiatorConnection,
            TargetUserId = GetReactionTargetUserId(reactorId, request.MessageId, request.SenderId, request.ReceiverId),
            Type = Contracts.Backplane.MessageType.Reaction,
            Reaction = new BackplaneReaction
            {
                ReactorId = reactorId,
                Reaction = request.Reaction
            }
        };

        return message;
    }

    public BackplaneMessage CreateBackplaneMessage(UnReactRequest request, string initiatorConnection, long reactorId)
    {
        BackplaneMessage message = new()
        {
            MessageId = request.MessageId,
            SenderId = request.SenderId,
            ReceiverId = request.ReceiverId,
            InitiatorConnection = initiatorConnection,
            TargetUserId = GetReactionTargetUserId(reactorId, request.MessageId, request.SenderId, request.ReceiverId),
            Type = Contracts.Backplane.MessageType.Reaction,
            Reaction = new BackplaneReaction
            {
                ReactorId = reactorId
            }
        };

        return message;
    }

    private static long GetReactionTargetUserId(long reactorId, long messageId, long senderId, long receiverId)
    {
        long targetUserId;

        if (reactorId == senderId)
        {
            targetUserId = receiverId;
        }
        else if (reactorId == receiverId)
        {
            targetUserId = senderId;
        }
        else
        {
            throw new InvalidOperationException($"Reactor {reactorId} to message {messageId} between {senderId}->{receiverId} should be one of the participants.");
        }

        return targetUserId;
    }

    public ListenNotification CreateListenNotification(SendMessageRequest request, long senderId, long messageId)
    {
        ListenNotification notification = new()
        {
            MessageId = messageId,
            SenderId = senderId,
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

    public ListenNotification CreateListenNotification(ReactRequest request, long reactorId)
    {
        ListenNotification notification = new()
        {
            MessageId = request.MessageId,
            SenderId = request.SenderId,
            ReceiverId = request.ReceiverId,
            Type = Contracts.Messaging.MessageType.Reaction,
            Reaction = new NotificationReaction
            {
                ReactorId = reactorId,
                Reaction = request.Reaction
            }
        };

        return notification;
    }

    public ListenNotification CreateListenNotification(UnReactRequest request, long reactorId)
    {
        ListenNotification notification = new()
        {
            MessageId = request.MessageId,
            SenderId = request.SenderId,
            ReceiverId = request.ReceiverId,
            Type = Contracts.Messaging.MessageType.Reaction,
            Reaction = new NotificationReaction
            {
                ReactorId = reactorId
            }
        };

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
            case Contracts.Backplane.MessageType.Connection:
                SetConnection(backplaneMessage, notification);
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

    private static void SetConnection(BackplaneMessage backplaneMessage, ListenNotification notification)
    {
        Contracts.Messaging.ConnectionStatus status;
        switch (backplaneMessage.Connection.Status)
        {
            case Contracts.Backplane.ConnectionStatus.NotConnected:
                status = Contracts.Messaging.ConnectionStatus.NotConnected;
                break;
            case Contracts.Backplane.ConnectionStatus.Pending:
                status = Contracts.Messaging.ConnectionStatus.Pending;
                break;
            case Contracts.Backplane.ConnectionStatus.Connected:
                status = Contracts.Messaging.ConnectionStatus.Connected;
                break;
            default:
                throw new EnumValueNotSupportedException(backplaneMessage.Connection.Status);
        }

        notification.Type = Contracts.Messaging.MessageType.Connection;
        notification.Connection = new NotificationConnection
        {
            Status = status,
            Version = backplaneMessage.Connection.Version.ToDateTime()
        };
    }
}
