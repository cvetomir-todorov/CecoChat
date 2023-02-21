using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.History;

namespace CecoChat.Server.History;

public interface IContractMapper
{
    DataMessage CreateDataMessage(BackplaneMessage backplaneMessage);

    ReactionMessage CreateReactionMessage(BackplaneMessage backplaneMessage);
}

public class ContractMapper : IContractMapper
{
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
