using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.Chats;

namespace CecoChat.Server.Chats;

public interface IContractMapper
{
    PlainTextMessage CreatePlainTextMessage(BackplaneMessage backplaneMessage);

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
