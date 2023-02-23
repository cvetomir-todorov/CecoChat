using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.Messaging;

namespace CecoChat.Server.Messaging;

public interface IContractMapper
{
    BackplaneMessage CreateBackplaneMessage(SendMessageRequest request, long senderId, string senderConnectionId, long messageId);

    BackplaneMessage CreateBackplaneMessage(ReactRequest request, string senderConnectionId, long reactorId);

    BackplaneMessage CreateBackplaneMessage(UnReactRequest request, string senderConnectionId, long reactorId);

    ListenNotification CreateListenNotification(SendMessageRequest request, long senderId, long messageId);

    ListenNotification CreateListenNotification(ReactRequest request, long reactorId);

    ListenNotification CreateListenNotification(UnReactRequest request, long reactorId);

    ListenNotification CreateListenNotification(BackplaneMessage backplaneMessage);
}

public class ContractMapper : IContractMapper
{
    public BackplaneMessage CreateBackplaneMessage(SendMessageRequest request, long senderId, string senderConnectionId, long messageId)
    {
        BackplaneMessage message = new()
        {
            MessageId = messageId,
            SenderId = senderId,
            ReceiverId = request.ReceiverId,
            SenderConnectionId = senderConnectionId,
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

    public BackplaneMessage CreateBackplaneMessage(ReactRequest request, string senderConnectionId, long reactorId)
    {
        BackplaneMessage message = new()
        {
            MessageId = request.MessageId,
            SenderId = request.SenderId,
            ReceiverId = request.ReceiverId,
            SenderConnectionId = senderConnectionId,
            Type = Contracts.Backplane.MessageType.Reaction,
            Reaction = new BackplaneReaction
            {
                ReactorId = reactorId,
                Reaction = request.Reaction
            }
        };

        return message;
    }

    public BackplaneMessage CreateBackplaneMessage(UnReactRequest request, string senderConnectionId, long reactorId)
    {
        BackplaneMessage message = new()
        {
            MessageId = request.MessageId,
            SenderId = request.SenderId,
            ReceiverId = request.ReceiverId,
            SenderConnectionId = senderConnectionId,
            Type = Contracts.Backplane.MessageType.Reaction,
            Reaction = new BackplaneReaction
            {
                ReactorId = reactorId
            }
        };

        return message;
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
}
