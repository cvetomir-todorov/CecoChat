using CecoChat.Contracts.Backplane;
using CecoChat.Messaging.Contracts;
using Common;

namespace CecoChat.Messaging.Service;

public interface IContractMapper
{
    BackplaneMessage CreateBackplaneMessage(SendPlainTextRequest request, long senderId, string initiatorConnection, long messageId);

    BackplaneMessage CreateBackplaneMessage(SendFileRequest request, long senderId, string initiatorConnection, long messageId);

    BackplaneMessage CreateBackplaneMessage(ReactRequest request, string initiatorConnection, long reactorId);

    BackplaneMessage CreateBackplaneMessage(UnReactRequest request, string initiatorConnection, long reactorId);

    ListenNotification CreateListenNotification(SendPlainTextRequest request, long senderId, long messageId);

    ListenNotification CreateListenNotification(SendFileRequest request, long senderId, long messageId);

    ListenNotification CreateListenNotification(ReactRequest request, long reactorId);

    ListenNotification CreateListenNotification(UnReactRequest request, long reactorId);

    ListenNotification CreateListenNotification(BackplaneMessage backplaneMessage);
}

public class ContractMapper : IContractMapper
{
    public BackplaneMessage CreateBackplaneMessage(SendPlainTextRequest request, long senderId, string initiatorConnection, long messageId)
    {
        BackplaneMessage message = new()
        {
            MessageId = messageId,
            SenderId = senderId,
            ReceiverId = request.ReceiverId,
            InitiatorConnection = initiatorConnection,
            TargetUserId = request.ReceiverId,
            Type = CecoChat.Contracts.Backplane.MessageType.PlainText,
            Status = CecoChat.Contracts.Backplane.DeliveryStatus.Processed,
            PlainText = new BackplanePlainText
            {
                Text = request.Text
            }
        };

        return message;
    }

    public BackplaneMessage CreateBackplaneMessage(SendFileRequest request, long senderId, string initiatorConnection, long messageId)
    {
        BackplaneMessage message = new()
        {
            MessageId = messageId,
            SenderId = senderId,
            ReceiverId = request.ReceiverId,
            InitiatorConnection = initiatorConnection,
            TargetUserId = request.ReceiverId,
            Type = CecoChat.Contracts.Backplane.MessageType.File,
            Status = CecoChat.Contracts.Backplane.DeliveryStatus.Processed,
            File = new BackplaneFile
            {
                Text = request.Text,
                Bucket = request.Bucket,
                Path = request.Path
            }
        };

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
            Type = CecoChat.Contracts.Backplane.MessageType.Reaction,
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
            Type = CecoChat.Contracts.Backplane.MessageType.Reaction,
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

    public ListenNotification CreateListenNotification(SendPlainTextRequest request, long senderId, long messageId)
    {
        ListenNotification notification = new()
        {
            MessageId = messageId,
            SenderId = senderId,
            ReceiverId = request.ReceiverId,
            Type = CecoChat.Messaging.Contracts.MessageType.PlainText,
            PlainText = new NotificationPlainText
            {
                Text = request.Text
            }
        };

        return notification;
    }

    public ListenNotification CreateListenNotification(SendFileRequest request, long senderId, long messageId)
    {
        ListenNotification notification = new()
        {
            MessageId = messageId,
            SenderId = senderId,
            ReceiverId = request.ReceiverId,
            Type = CecoChat.Messaging.Contracts.MessageType.File,
            File = new NotificationFile
            {
                Text = request.Text,
                Bucket = request.Bucket,
                Path = request.Path
            }
        };

        return notification;
    }

    public ListenNotification CreateListenNotification(ReactRequest request, long reactorId)
    {
        ListenNotification notification = new()
        {
            MessageId = request.MessageId,
            SenderId = request.SenderId,
            ReceiverId = request.ReceiverId,
            Type = CecoChat.Messaging.Contracts.MessageType.Reaction,
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
            Type = CecoChat.Messaging.Contracts.MessageType.Reaction,
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
            case CecoChat.Contracts.Backplane.MessageType.PlainText:
                SetPlainText(backplaneMessage, notification);
                break;
            case CecoChat.Contracts.Backplane.MessageType.File:
                SetFile(backplaneMessage, notification);
                break;
            case CecoChat.Contracts.Backplane.MessageType.Reaction:
                SetReaction(backplaneMessage, notification);
                break;
            case CecoChat.Contracts.Backplane.MessageType.Connection:
                SetConnection(backplaneMessage, notification);
                break;
            default:
                throw new EnumValueNotSupportedException(backplaneMessage.Type);
        }

        return notification;
    }

    private static void SetPlainText(BackplaneMessage backplaneMessage, ListenNotification notification)
    {
        notification.Type = CecoChat.Messaging.Contracts.MessageType.PlainText;
        notification.PlainText = new NotificationPlainText
        {
            Text = backplaneMessage.PlainText.Text
        };
    }

    private static void SetFile(BackplaneMessage backplaneMessage, ListenNotification notification)
    {
        notification.Type = CecoChat.Messaging.Contracts.MessageType.File;
        notification.File = new NotificationFile
        {
            Text = backplaneMessage.File.Text,
            Bucket = backplaneMessage.File.Bucket,
            Path = backplaneMessage.File.Path
        };
    }

    private static void SetReaction(BackplaneMessage backplaneMessage, ListenNotification notification)
    {
        notification.Type = CecoChat.Messaging.Contracts.MessageType.Reaction;
        notification.Reaction = new NotificationReaction
        {
            ReactorId = backplaneMessage.Reaction.ReactorId,
            Reaction = backplaneMessage.Reaction.Reaction
        };
    }

    private static void SetConnection(BackplaneMessage backplaneMessage, ListenNotification notification)
    {
        CecoChat.Messaging.Contracts.ConnectionStatus status;
        switch (backplaneMessage.Connection.Status)
        {
            case CecoChat.Contracts.Backplane.ConnectionStatus.NotConnected:
                status = CecoChat.Messaging.Contracts.ConnectionStatus.NotConnected;
                break;
            case CecoChat.Contracts.Backplane.ConnectionStatus.Pending:
                status = CecoChat.Messaging.Contracts.ConnectionStatus.Pending;
                break;
            case CecoChat.Contracts.Backplane.ConnectionStatus.Connected:
                status = CecoChat.Messaging.Contracts.ConnectionStatus.Connected;
                break;
            default:
                throw new EnumValueNotSupportedException(backplaneMessage.Connection.Status);
        }

        notification.Type = CecoChat.Messaging.Contracts.MessageType.Connection;
        notification.Connection = new NotificationConnection
        {
            Status = status,
            Version = backplaneMessage.Connection.Version.ToDateTime()
        };
    }
}
