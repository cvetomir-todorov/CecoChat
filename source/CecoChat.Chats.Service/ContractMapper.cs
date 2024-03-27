using CecoChat.Chats.Contracts;
using CecoChat.Contracts.Backplane;

namespace CecoChat.Chats.Service;

public interface IContractMapper
{
    PlainTextMessage CreatePlainTextMessage(BackplaneMessage backplaneMessage);

    FileMessage CreateFileMessage(BackplaneMessage backplaneMessage);

    ReactionMessage CreateReactionMessage(BackplaneMessage backplaneMessage);
}

public class ContractMapper : IContractMapper
{
    public PlainTextMessage CreatePlainTextMessage(BackplaneMessage backplaneMessage)
    {
        if (backplaneMessage.Type != MessageType.PlainText)
        {
            throw new ArgumentException($"Message should be of type {MessageType.PlainText}.");
        }

        PlainTextMessage plainTextMessage = new()
        {
            MessageId = backplaneMessage.MessageId,
            SenderId = backplaneMessage.SenderId,
            ReceiverId = backplaneMessage.ReceiverId,
            Text = backplaneMessage.PlainText.Text
        };

        return plainTextMessage;
    }

    public FileMessage CreateFileMessage(BackplaneMessage backplaneMessage)
    {
        if (backplaneMessage.Type != MessageType.File)
        {
            throw new ArgumentException($"Message should be of type {MessageType.File}.");
        }

        FileMessage fileMessage = new()
        {
            MessageId = backplaneMessage.MessageId,
            SenderId = backplaneMessage.SenderId,
            ReceiverId = backplaneMessage.ReceiverId,
            Text = backplaneMessage.File.Text,
            Bucket = backplaneMessage.File.Bucket,
            Path = backplaneMessage.File.Path
        };

        return fileMessage;
    }

    public ReactionMessage CreateReactionMessage(BackplaneMessage backplaneMessage)
    {
        if (backplaneMessage.Type != MessageType.Reaction)
        {
            throw new ArgumentException($"Message should be of type {MessageType.Reaction}.");
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
