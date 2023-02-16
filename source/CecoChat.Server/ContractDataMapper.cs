using CecoChat.Contracts;
using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.History;
using CecoChat.Contracts.Messaging;

namespace CecoChat.Server;

public interface IContractDataMapper
{
    BackplaneMessage CreateBackplaneMessage(SendMessageRequest request, long senderId, Guid senderClientId, long messageId);

    BackplaneMessage CreateBackplaneMessage(ReactRequest request, Guid senderClientId, long reactorId);

    BackplaneMessage CreateBackplaneMessage(UnReactRequest request, Guid senderClientId, long reactorId);

    ListenNotification CreateListenNotification(SendMessageRequest request, long senderId, long messageId);

    ListenNotification CreateListenNotification(ReactRequest request, long reactorId);

    ListenNotification CreateListenNotification(UnReactRequest request, long reactorId);

    ListenNotification CreateListenNotification(BackplaneMessage backplaneMessage);

    DataMessage CreateDataMessage(BackplaneMessage backplaneMessage);

    ReactionMessage CreateReactionMessage(BackplaneMessage backplaneMessage);
}

public class ContractDataMapper : IContractDataMapper
{
    public BackplaneMessage CreateBackplaneMessage(SendMessageRequest request, long senderId, Guid senderClientId, long messageId)
    {
        BackplaneMessage message = new()
        {
            MessageId = messageId,
            SenderId = senderId,
            ReceiverId = request.ReceiverId,
            ClientId = senderClientId.ToUuid(),
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

    public BackplaneMessage CreateBackplaneMessage(ReactRequest request, Guid senderClientId, long reactorId)
    {
        BackplaneMessage message = new()
        {
            MessageId = request.MessageId,
            SenderId = request.SenderId,
            ReceiverId = request.ReceiverId,
            ClientId = senderClientId.ToUuid(),
            Type = Contracts.Backplane.MessageType.Reaction,
            Reaction = new BackplaneReaction
            {
                ReactorId = reactorId,
                Reaction = request.Reaction
            }
        };

        return message;
    }

    public BackplaneMessage CreateBackplaneMessage(UnReactRequest request, Guid senderClientId, long reactorId)
    {
        BackplaneMessage message = new()
        {
            MessageId = request.MessageId,
            SenderId = request.SenderId,
            ReceiverId = request.ReceiverId,
            ClientId = senderClientId.ToUuid(),
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